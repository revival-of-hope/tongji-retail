## backend01
## backend02
## backend03
## frontend01

### OpenAPI 路由

我理解到这个项目采用的是“契约优先”的前后端协作方式。后端把接口路径、请求参数、响应结构和 `operationId` 统一写在 `backend/openapi/retail-system.json` 中，前端不需要再手写一套接口定义。当前接口按认证、商品、购物车、订单、商家、工单、报表和管理员功能分组，每个公开接口都有唯一的 `operationId`，HeyAPI 会用它生成对应的 TypeScript 函数。

例如几条我重点梳理过的调用关系：

| HTTP 路由 | operationId | SDK 中的调用 |
| --- | --- | --- |
| `GET /api/products` | `GetProducts` | `api.products(query)` |
| `GET /api/products/{id}` | `GetProduct` | `api.product(id)` |
| `POST /api/cart/items` | `AddCartItem` | `api.addCartItem(body)` |
| `POST /api/orders` | `CreateOrder` | `api.createOrder(body)` |
| `POST /api/orders/{id}/pay` | `PayOrder` | `api.payOrder(id, body)` |
| `PUT /api/merchants/{id}/review` | `ReviewMerchant` | `api.reviewMerchant(id, body)` |

路径中的 `{id}`、`{cartItemId}` 会生成 `path` 参数，请求体会生成 `body` 参数，商品查询中的关键词、分类、价格和排序等条件会生成 `query` 类型。这样在页面调用接口时，如果参数名或类型写错，TypeScript 在编译阶段就能发现。

### HeyAPI 生成目录

`frontend/openapi-ts.config.ts` 指定输入为后端的 OpenAPI JSON，输出到 `frontend/lib/api/generated`，并启用了 Fetch 客户端、TypeScript 类型和 SDK 三个插件。运行 `pnpm generate:api` 后，主要生成内容如下：

- `types.gen.ts`：请求 DTO、响应 DTO、路径参数和查询参数类型。
- `sdk.gen.ts`：按照 `operationId` 生成的底层请求函数，例如 `getProducts`、`createOrder`。
- `client.gen.ts` 和 `client/`：Fetch 客户端实例及请求处理代码。
- `core/`：参数、路径、请求体和认证等通用序列化逻辑。

我不会直接修改 `generated` 目录，因为下次执行生成命令时手工修改会被覆盖。正确流程是后端更新 OpenAPI 文件后，前端重新执行 `pnpm generate:api`，再运行 `pnpm typecheck` 检查受影响的调用。

### `sdk.ts` 的作用

`frontend/lib/api/sdk.ts` 是生成代码和页面之间的手写适配层。文件开头分别引入生成的客户端、底层 SDK 函数和 DTO 类型。`client.setConfig` 完成两项公共配置：一是从 `NEXT_PUBLIC_API_URL` 读取后端地址，未配置时使用 `http://localhost:8080`；二是在浏览器环境中读取 LocalStorage 里的 `retail-access-token`，交给生成客户端添加认证信息。

后端响应统一使用 `{ code, message, data }` 信封，所以 `unwrap<T>` 会集中判断错误响应、空响应和空数据，失败时抛出带 HTTP 状态码的 `ApiError`，成功时只把真正的 `data` 返回给页面。最下面的 `api` 对象进一步把生成函数包装成更适合业务页面使用的方法。例如页面调用 `api.product(id)` 即可，内部再转换为 `generated.getProduct({ path: { id } })`；调用 `api.createOrder(body)` 时，请求和返回值会分别受到 `CreateOrderRequest`、`OrderDetail` 类型约束。

经过这次阅读，我可以从一个页面中的 `api.xxx()` 调用，继续追踪到 `sdk.ts`、`sdk.gen.ts`，最后找到 OpenAPI 中对应的 HTTP 方法和路径。我也理解了生成代码负责“严格对应接口契约”，手写 `sdk.ts` 负责“统一配置和提供易用的业务调用”这一层次划分。

## frontend02
## frontend03
