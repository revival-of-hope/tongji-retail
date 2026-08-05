# Tongji-Retail

本系统以电商平台为业务背景，采用前后端分离架构（C# ASP.NET Core 9 + EntityFrameworkCore 9;  Next.js 14 + Shadcn UI组件库; Oracle 21c）。系统涵盖顾客、商家、管理员、客服四大角色。使用docker compose进行部署
## 项目分工
| Docker 与统筹 | 前端              | 后端             | 数据库设计与文档撰写 |
| ------------- | ----------------- | ---------------- | -------------------- |
| 周禹佟        | frontend01 宋博文 | backend01 宁子谦 | 熊庭楷               |
|               | frontend02 李晨恺 | backend02 刘礼嘉 | 赖浩翔               |
|               | frontend03 刘子康 | backend03 付林轩 | 杜冰焱               |

>请Fork后在本地完成对应功能后再提交到自己的Fork仓库,最后再提交Pull Request就可以了.


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
# 若已有数据卷还需先运行以下命令来删除
# docker compose down -v
docker compose up --build
```

启动后：

- 前端：http://localhost:3000
- 后端：http://localhost:8080
- OpenAPI：http://localhost:8080/openapi/v1.json
- Oracle：localhost:1521/XEPDB1

首次启动 Oracle 需要初始化数据库。后端会等待数据库健康后自动建表并写入演示数据。

## 演示账号

| 角色   | 用户名     | 密码           |
| ------ | ---------- | -------------- |
| 管理员 | `admin`    | `Admin123!`    |
| 顾客   | `customer` | `Customer123!` |
| 商家   | `merchant` | `Merchant123!` |
| 客服   | `service`  | `Service123!`  |

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
sh ./scripts/test-all.sh
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

容器启动后的完整业务冒烟测试：

```bash
docker compose up --build --detach --wait
sh ./scripts/smoke-test.sh
```

仓库内置 `.github/workflows/ci.yml`，会在推送和拉取请求时执行后端构建与测试、OpenAPI/生成客户端漂移检查、前端 lint/类型检查/测试/构建，以及 Oracle 端到端冒烟测试。
