# 系统架构

## 组件

```text
Browser
  ├─ http://localhost:3000 → Next.js 16 standalone server
  └─ http://localhost:8080 → ASP.NET Core 9 Minimal API
                                      │
                                      └─ Oracle XE 21c / XEPDB1
```

系统不配置反向代理。浏览器直接调用后端公开端口，后端以 CORS 限制允许的前端来源。

## 后端结构

- `Contracts`：OpenAPI 请求、响应与统一信封模型
- `Models`：12 张表的 EF Core 实体与状态枚举
- `Data`：Oracle 映射、自动建表和演示数据
- `Endpoints`：按认证、商品、购物车、订单、商家、工单、报表、管理拆分的 Minimal API
- `Services`：JWT、映射、当前用户和输入验证
- `Middleware`：统一异常响应

所有公开端点具有唯一 `operationId`。受保护端点使用角色策略，密码使用 BCrypt 哈希。

## 前端结构

- `app`：Next.js App Router 页面，按顾客、商家、客服、管理员分区
- `components/ui`：shadcn/ui 基础组件
- `components`：只组合业务页面需要的导航、鉴权和展示组件
- `lib/api/generated`：由 OpenAPI 自动生成，禁止手工修改
- `lib/api/sdk.ts`：统一配置 base URL、JWT 和响应信封解包
- `store/auth.ts`：Zustand 登录状态

## 数据一致性

- 下单写入订单项价格快照。
- 支付和已支付订单取消均使用串行化事务。
- 支付时再次检查商品状态和库存，以减少超卖风险。
- 评价后重新计算商品评价数与平均分。
- 商品修改一律重新进入审核。
