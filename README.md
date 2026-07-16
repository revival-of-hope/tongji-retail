# 商品零售管理系统

一套前后端分离的课程项目，覆盖顾客、商家、管理员、客服四类角色。接口以 ASP.NET Core 生成的 OpenAPI 文档为唯一契约，前端客户端由该契约自动生成。

## 技术栈

- 前端：Next.js 16、React 19、TypeScript、Tailwind CSS 4、shadcn/ui、Zustand、Recharts、Vitest
- 后端：ASP.NET Core 9 Minimal API、EF Core 9、Oracle Entity Framework Core、JWT、BCrypt、xUnit
- 数据库：Oracle Database XE 21c
- 部署：Docker Compose；不使用 Nginx 或其他反向代理

## 已实现业务

- 顾客：注册登录、商品搜索筛选、详情与评价、购物车、结算、模拟支付、订单取消/确认收货、评价、客服工单、商家入驻申请
- 商家：商品发布与修改、商品审核状态查看、订单发货、销售报表
- 管理员：平台概览、商家审核、商品审核、用户/订单/工单查看及工单处理
- 客服：自动分配工单、回复与状态更新
- 数据：固定 12 张核心表，包含价格快照、支付后扣减库存、退款恢复库存、评价聚合和树形分类

## 一键启动

```bash
cp .env.example .env
docker compose up --build
```

启动后：

- 前端：http://localhost:3000
- 后端：http://localhost:8080
- OpenAPI：http://localhost:8080/openapi/v1.json
- Oracle：localhost:1521/XEPDB1

首次启动 Oracle 需要初始化数据库。后端会等待数据库健康后自动建表并写入演示数据。

## 演示账号

| 角色 | 用户名 | 密码 |
|---|---|---|
| 管理员 | `admin` | `Admin123!` |
| 顾客 | `customer` | `Customer123!` |
| 商家 | `merchant` | `Merchant123!` |
| 客服 | `service` | `Service123!` |

商家申请审核通过后，旧 JWT 中仍保留原角色，申请人需要重新登录以取得商家权限。

## OpenAPI 工作流

后端端点通过 `WithName` 提供稳定 `operationId`，构建时输出 `backend/openapi/retail-system.json`。前端不得手写接口路径或请求/响应类型：

```bash
cd frontend
pnpm generate:api
```

生成结果位于 `frontend/lib/api/generated`，业务页面统一通过 `frontend/lib/api/sdk.ts` 调用。

## 测试

完整本地检查：

```bash
./scripts/test-all.sh
```

也可分别执行：

```bash
cd frontend
pnpm lint
pnpm typecheck
pnpm test
pnpm build

cd ../backend
dotnet test test/RetailSystem.Api.Tests.csproj -c Release
```

容器启动后的冒烟测试：

```bash
./scripts/smoke-test.sh
```

## 关键业务约束

一个订单只允许包含同一商家的商品。现有 12 表结构没有订单—商家拆单表，该约束可保证单个订单的发货责任和状态流转唯一；跨商家商品需分开结算。

更多说明见：

- `docs/requirements.md`
- `docs/architecture.md`
- `docs/testing.md`
- `backend/docs/database.md`
- `frontend/docs/openapi-client.md`

## 角色导航说明

- 顾客直接使用商城、购物车、订单、客服工单和商家入驻页面，不设置独立工作台。
- `/merchant/apply` 是顾客可访问的独立申请页，不继承商家工作台的权限布局。
- 商家工作台仍位于 `/merchant`，仅审核通过后的商家账号可访问。
- 客服工作台显示分配给当前客服以及尚未分配的工单；客服回复未分配工单时自动接单。

## Merchant route layout note

The `/merchant` dashboard routes and `/merchant/apply` customer application route must remain under the same `frontend/app/merchant` route tree. Next.js 16 does not allow the same URL segment tree to be split across route groups such as `(merchant-dashboard)/merchant` and `(shop)/merchant`.
