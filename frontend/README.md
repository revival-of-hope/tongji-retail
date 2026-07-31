# Tongji Retail Web

Next.js 16 + React 19 + TypeScript + Tailwind CSS 4 + shadcn/ui。

## 本地运行

```bash
pnpm install
pnpm generate:api
pnpm dev
```

浏览器后端地址由 `NEXT_PUBLIC_API_URL` 设置，默认使用
`http://localhost:8080`。

## 质量检查

```bash
pnpm lint
pnpm typecheck
pnpm build
```

## 项目约定

- App Router 结构说明：`docs/app-router.md`
- OpenAPI 客户端说明：`docs/openapi-client.md`
- 页面只通过 `lib/api/sdk.ts` 访问后端。
- `lib/api/generated` 是自动生成目录，不要手动编辑。
