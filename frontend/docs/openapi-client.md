# OpenAPI 客户端维护

前端 API 类型和请求函数由 `backend/openapi/retail-system.json` 生成。业务页面只应通过 `frontend/lib/api/sdk.ts` 和 `frontend/lib/api/generated` 访问后端，不要在页面中手写重复的接口类型。

## 更新流程

1. 修改后端契约或端点。
2. 构建后端，使 OpenAPI 文档写入 `backend/openapi/retail-system.json`。
3. 在 `frontend` 目录运行 `pnpm generate:api`。
4. 运行 `pnpm lint`、`pnpm typecheck`、`pnpm test` 和 `pnpm build`。
5. 提交 OpenAPI 文档和生成客户端的差异。

GitHub Actions 会同时检查后端生成文档和前端生成客户端是否存在未提交差异。直接手改生成目录会在 CI 中失败。
