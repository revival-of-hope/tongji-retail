# 测试与交付检查

## 一键本地检查

安装 .NET 9 SDK、Node.js 22、pnpm 10.12.1 后，在项目根目录执行：

```bash
sh ./scripts/test-all.sh
```

脚本依次执行后端恢复、构建、OpenAPI 漂移检查、后端测试，以及前端客户端生成、lint、类型检查、单元测试和生产构建。

## Oracle 端到端冒烟测试

Docker 环境中执行：

```bash
docker compose up --build --detach --wait
sh ./scripts/smoke-test.sh
docker compose down --volumes
```

冒烟测试会使用随机顾客账号完成以下真实流程：注册、加购、下单、支付、商家发货、确认收货、评价、客服处理工单、管理员关闭工单、库存核对和报表核对。脚本依赖 `curl` 与 `jq`。

数据库约束发生变化后，应使用新的 Oracle 数据卷；`EnsureCreated` 不会向既有数据库自动追加约束。开发环境可执行：

```bash
docker compose down --volumes
docker compose up --build
```

## GitHub Actions

`.github/workflows/ci.yml` 包含三个作业：

- 后端构建、完整契约比较和单元/集成测试；
- 前端生成客户端、lint、类型检查、单元测试和生产构建；
- 启动 Oracle、后端和前端后执行完整业务冒烟测试。

任何作业失败时均不应合并代码。Docker 作业失败会上传容器日志。
