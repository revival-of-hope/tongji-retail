#!/usr/bin/env sh
set -eu

API_URL="${API_URL:-http://localhost:8080}"
WEB_URL="${WEB_URL:-http://localhost:3000}"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

command -v curl >/dev/null 2>&1 || { echo "curl is required" >&2; exit 1; }
command -v jq >/dev/null 2>&1 || { echo "jq is required" >&2; exit 1; }

request() {
  method="$1"
  path="$2"
  token="${3:-}"
  body="${4:-}"
  output="$TMP_DIR/response.json"

  set -- --silent --show-error --fail-with-body --request "$method" \
    --header "Accept: application/json" --output "$output"
  if [ -n "$token" ]; then set -- "$@" --header "Authorization: Bearer $token"; fi
  if [ -n "$body" ]; then
    set -- "$@" --header "Content-Type: application/json" --data "$body"
  fi

  if ! curl "$@" "$API_URL$path"; then
    echo "Request failed: $method $path" >&2
    cat "$output" >&2 2>/dev/null || true
    exit 1
  fi
  cat "$output"
}

assert_envelope() {
  jq -e '.code >= 200 and .code < 300 and .data != null' >/dev/null
}

login() {
  username="$1"
  password="$2"
  request POST /api/auth/login "" "$(jq -nc --arg u "$username" --arg p "$password" '{username:$u,password:$p}')" \
    | jq -er '.data.accessToken'
}

curl --silent --show-error --fail "$API_URL/health" >/dev/null
curl --silent --show-error --fail "$API_URL/openapi/v1.json" | jq -e '.openapi and .paths' >/dev/null
curl --silent --show-error --fail "$WEB_URL" >/dev/null

suffix="$(date +%s)-$$"
username="smoke-$suffix"
password="SmokeTest123!"
register_body="$(jq -nc --arg u "$username" --arg p "$password" --arg e "$username@example.test" '{username:$u,password:$p,email:$e,phone:null}')"
customer_token="$(request POST /api/auth/register "" "$register_body" | jq -er '.data.accessToken')"
merchant_token="$(login merchant Merchant123!)"
service_token="$(login service Service123!)"
admin_token="$(login admin Admin123!)"

products="$(request GET '/api/products/?pageIndex=1&pageSize=1&sortBy=newest')"
echo "$products" | assert_envelope
product_id="$(echo "$products" | jq -er '.data.items[0].id')"
initial_stock="$(echo "$products" | jq -er '.data.items[0].stockQuantity')"

cart_item="$(request POST /api/cart/items "$customer_token" "$(jq -nc --argjson id "$product_id" '{productId:$id,quantity:1}')")"
cart_item_id="$(echo "$cart_item" | jq -er '.data.cartItemId')"

order_body="$(jq -nc --argjson id "$cart_item_id" '{cartItemIds:[$id],shippingAddress:"GitHub Actions 测试地址",remark:"automated smoke test"}')"
order="$(request POST /api/orders/ "$customer_token" "$order_body")"
order_id="$(echo "$order" | jq -er '.data.id')"
order_total="$(echo "$order" | jq -er '.data.totalAmount')"
echo "$order" | jq -e '.data.status == "PendingPayment"' >/dev/null

paid="$(request POST "/api/orders/$order_id/pay" "$customer_token" '{"paymentMethod":"Alipay"}')"
echo "$paid" | jq -e '.data.status == "PendingShipment" and .data.payment.status == "Success"' >/dev/null

shipped="$(request PUT "/api/orders/$order_id/ship" "$merchant_token")"
echo "$shipped" | jq -e '.data.status == "Shipped"' >/dev/null

completed="$(request PUT "/api/orders/$order_id/complete" "$customer_token")"
echo "$completed" | jq -e '.data.status == "Completed"' >/dev/null

review="$(request POST "/api/products/$product_id/reviews" "$customer_token" "$(jq -nc --argjson id "$order_id" '{orderId:$id,rating:5,comment:"automated smoke review"}')")"
echo "$review" | jq -e '.data.rating == 5' >/dev/null

orders="$(request GET /api/orders/ "$customer_token")"
echo "$orders" | jq -e --argjson oid "$order_id" --argjson pid "$product_id" \
  'any(.data[]; .id == $oid and any(.items[]; .productId == $pid and .reviewed == true))' >/dev/null

ticket="$(request POST /api/tickets/ "$customer_token" "$(jq -nc --argjson id "$order_id" '{orderId:$id,subject:"自动化工单",description:"验证客服与管理员工单处理链路"}')")"
ticket_id="$(echo "$ticket" | jq -er '.data.id')"

assigned="$(request GET /api/tickets/assigned "$service_token")"
echo "$assigned" | jq -e --argjson id "$ticket_id" 'any(.data[]; .id == $id)' >/dev/null

resolved="$(request PUT "/api/tickets/$ticket_id/reply" "$service_token" '{"reply":"客服已处理","status":"Resolved"}')"
echo "$resolved" | jq -e '.data.status == "Resolved"' >/dev/null

closed="$(request PUT "/api/tickets/$ticket_id/reply" "$admin_token" '{"reply":"管理员复核并关闭","status":"Closed"}')"
echo "$closed" | jq -e '.data.status == "Closed"' >/dev/null

product_after="$(request GET "/api/products/$product_id")"
final_stock="$(echo "$product_after" | jq -er '.data.stockQuantity')"
[ "$final_stock" -eq "$((initial_stock - 1))" ] || {
  echo "Inventory assertion failed: initial=$initial_stock final=$final_stock" >&2
  exit 1
}

merchant_report="$(request GET /api/reports/merchant "$merchant_token")"
echo "$merchant_report" | jq -e --argjson total "$order_total" '.data.totalSales >= $total and .data.totalOrders >= 1' >/dev/null

overview="$(request GET /api/reports/overview "$admin_token")"
echo "$overview" | jq -e '.data.totalOrders >= 1 and .data.totalSales > 0 and .data.totalUsers >= 5' >/dev/null

printf 'End-to-end smoke test passed for order %s, product %s and ticket %s.\n' "$order_id" "$product_id" "$ticket_id"
