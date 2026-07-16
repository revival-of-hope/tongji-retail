# OpenAPI 客户端约束

## 唯一契约

`backend/openapi/retail-system.json` 是前端请求类型和端点的唯一来源。`api.md`、手写 `api.js` 和文档截图不参与实现。

## 生成

```bash
cd frontend
pnpm generate:api
```

配置位于 `openapi-ts.config.ts`，使用 `@hey-api/openapi-ts` 生成 Fetch 客户端、SDK 和 TypeScript 类型。

## 使用规则

- 页面不得手写 `/api/...` 路径。
- 页面不得复制后端 DTO 类型。
- 页面只调用 `lib/api/sdk.ts` 暴露的业务方法。
- `lib/api/generated` 只能由生成命令修改。
- JWT 由 SDK 客户端配置从 LocalStorage 读取并附加到请求。
- 后端统一响应 `{ code, message, data }`，由 SDK 统一解包并转换为 `ApiError`。
