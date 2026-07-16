#!/usr/bin/env sh
set -eu

API_URL="${API_URL:-http://localhost:8080}"
WEB_URL="${WEB_URL:-http://localhost:3000}"

curl --fail --silent "$API_URL/health" >/dev/null
curl --fail --silent "$API_URL/openapi/v1.json" >/dev/null
curl --fail --silent "$API_URL/api/products/?pageSize=1" >/dev/null
curl --fail --silent "$WEB_URL" >/dev/null
printf 'Smoke tests passed: %s and %s\n' "$API_URL" "$WEB_URL"
