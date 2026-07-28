# OpenAPI 客户端使用约定

## 唯一接口契约

`backend/openapi/retail-system.json` 是前端请求类型和接口路径的唯一来源。
页面中不再手写 `/api/...` 路径，也不手动复制后端 DTO 类型。

## 重新生成客户端

后端接口契约更新后，在 `frontend` 目录运行：

```bash
pnpm generate:api
```

生成配置位于 `openapi-ts.config.ts`，输出目录为
`lib/api/generated`。该目录中的文件由 HeyAPI 生成，不应手动修改。

## 页面调用规则

- 页面只调用 `lib/api/sdk.ts` 暴露的业务方法。
- JWT 由 SDK 客户端从 LocalStorage 读取并自动附加到请求。
- 后端统一响应 `{ code, message, data }`，由 SDK 解包。
- 失败响应统一转换为带 HTTP 状态码的 `ApiError`。
