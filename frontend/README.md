# Retail System Web

Next.js 16 + React 19 + TypeScript + Tailwind CSS 4 + shadcn/ui。

```bash
pnpm install
pnpm generate:api
pnpm dev
```

质量检查：

```bash
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

前端只通过 `lib/api/sdk.ts` 使用 OpenAPI 生成客户端。浏览器后端地址由 `NEXT_PUBLIC_API_URL` 设置，默认 `http://localhost:8080`。
