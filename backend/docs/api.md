## 认证

```text
POST /api/auth/register
POST /api/auth/login
```

## 商品

```text
GET  /api/products
GET  /api/products/{id}
POST /api/products
PUT  /api/products/{id}/review
GET  /api/categories
```

## 购物车与订单

```text
GET    /api/orders/cart
POST   /api/orders/cart
PUT    /api/orders/cart/{cartItemId}
DELETE /api/orders/cart/{cartItemId}

POST   /api/orders
GET    /api/orders
GET    /api/orders/{id}
POST   /api/orders/pay
PUT    /api/orders/{id}/ship
PUT    /api/orders/{id}/complete
PUT    /api/orders/{id}/cancel
```

## 商家

```text
POST /api/merchants/apply
GET  /api/merchants/pending
PUT  /api/merchants/{id}/approve
GET  /api/merchants/my-products
GET  /api/merchants/my-orders
```

## 管理与报表

```text
GET /api/reports/overview
GET /api/reports/daily-sales
GET /api/reports/category-sales
GET /api/reports/merchant
```

## 客服工单

```text
POST /api/tickets
GET  /api/tickets/my
GET  /api/tickets/assigned
PUT  /api/tickets/{id}/reply
GET  /api/tickets/all
```

