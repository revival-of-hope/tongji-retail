// 认证
api.post("/api/auth/login", data);
api.post("/api/auth/register", data);

// 商品
api.get("/api/products");
api.get(`/api/products/${id}`);
api.post("/api/products", data);
api.put(`/api/products/${id}/review`, data);
api.get("/api/categories");

// 购物车
api.get("/api/orders/cart");
api.post("/api/orders/cart", data);
api.put(`/api/orders/cart/${cartItemId}`, data);
api.delete(`/api/orders/cart/${cartItemId}`);

// 订单
api.post("/api/orders", data);
api.get("/api/orders");
api.get(`/api/orders/${id}`);
api.post("/api/orders/pay", data);
api.put(`/api/orders/${id}/cancel`);
api.put(`/api/orders/${id}/complete`);
api.put(`/api/orders/${id}/ship`);

// 商家
api.post("/api/merchants/apply", data);
api.get("/api/merchants/pending");
api.put(`/api/merchants/${id}/approve`, data);
api.get("/api/merchants/my-products");
api.get("/api/merchants/my-orders");

// 报表
api.get("/api/reports/overview");
api.get("/api/reports/daily-sales");
api.get("/api/reports/category-sales");
api.get("/api/reports/merchant");

// 工单
api.post("/api/tickets", data);
api.get("/api/tickets/my");
api.get("/api/tickets/assigned");
api.put(`/api/tickets/${id}/reply`, data);
api.get("/api/tickets/all");