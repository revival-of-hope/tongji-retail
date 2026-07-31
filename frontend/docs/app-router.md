# App Router 路由说明

项目使用 Next.js App Router，页面由 `app` 目录中的文件结构决定。

## 使用的路由约定

- `(shop)` 是路由组，只用于组织顾客页面，不出现在 URL 中。
- `[id]` 是动态路由段，用于商品和订单编号。
- `admin/layout.tsx` 与 `merchant/layout.tsx` 是嵌套布局。
- `_components` 是私有目录，不会生成页面路由。

## 页面范围

| 模块     | 路由                                                                                               |
| -------- | -------------------------------------------------------------------------------------------------- |
| 商城     | `/`、`/products/[id]`、`/cart`                                                                     |
| 顾客订单 | `/orders`、`/orders/checkout`、`/orders/[id]/pay`                                                  |
| 顾客服务 | `/tickets`                                                                                         |
| 认证     | `/auth/login`、`/auth/register`                                                                    |
| 商家     | `/merchant`、`/merchant/apply`、`/merchant/products`、`/merchant/orders`                           |
| 管理员   | `/admin`、`/admin/users`、`/admin/merchants`、`/admin/products`、`/admin/orders`、`/admin/tickets` |
| 客服     | `/cs`                                                                                              |

当前页面内容是路由骨架，后续 UI 任务可以直接在对应 `page.tsx` 中实现。
