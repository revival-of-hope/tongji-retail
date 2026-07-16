# 测试说明

## 前端

Vitest + Testing Library 覆盖：

- 金额、日期和状态格式化
- 商品卡片渲染
- Zustand 登录状态持久化与退出

同时执行 ESLint、TypeScript 严格类型检查和 Next.js 生产构建。

## 后端

xUnit + `WebApplicationFactory` 启动真实 Minimal API 管线，测试数据库替换为 EF Core InMemory。覆盖：

- 注册、登录、当前用户与匿名访问拦截
- 顾客创建商品的角色越权拦截
- 商家修改商品后重新审核，以及待审商品的所有者可见性
- 购物车、下单、支付、发货完整流程
- 已支付订单取消后的退款状态
- 跨商家商品禁止在同一订单结算
- 检入 OpenAPI 的 `operationId` 唯一性
- 运行时 OpenAPI 与检入契约的 `operationId` 一致性

## Oracle 冒烟验证

单元/接口测试用于快速验证业务管线；Oracle 实际映射通过 Compose 启动后执行：

```bash
docker compose up --build -d
./scripts/smoke-test.sh
```

后端镜像构建阶段会先执行 xUnit，前端镜像构建阶段会依次执行 lint、类型检查、Vitest 和生产构建。
