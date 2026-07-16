# Oracle 数据库设计

系统固定使用 12 张核心表：

1. `USERS`
2. `MERCHANTS`
3. `CATEGORIES`
4. `PRODUCTS`
5. `PRODUCT_IMAGES`
6. `SHOPPING_CARTS`
7. `CART_ITEMS`
8. `ORDERS`
9. `ORDER_ITEMS`
10. `PAYMENTS`
11. `PRODUCT_REVIEWS`
12. `CUSTOMER_SERVICE_TICKETS`

## 主要关系

- 用户与商家、购物车均为一对零或一。
- 分类通过 `ParentId` 形成树形关系。
- 商家、分类分别与商品一对多；商品与图片一对多。
- 购物车与明细、订单与明细均为一对多。
- 订单与支付为一对零或一。
- 评价关联用户、订单、商品，`OrderId + ProductId` 唯一。
- 工单关联提交用户、可选订单和可选客服用户。

## Oracle 映射

- 主键使用 `ValueGeneratedOnAdd`，由 Oracle Provider 创建兼容的自动生成列。
- 金额字段映射为 `NUMBER(18,2)`，评分映射为 `NUMBER(3,2)`。
- 枚举映射为 `NUMBER(10)`，布尔值映射为 `NUMBER(1)`。
- 商品描述映射为 `CLOB`。
- 删除行为显式设置，避免订单、商品等历史数据被级联删除。

Compose 使用 `gvenzl/oracle-xe:21-slim-faststart`，默认服务名为 `XEPDB1`，应用用户通过 `APP_USER` 和 `APP_USER_PASSWORD` 首次启动时创建。
