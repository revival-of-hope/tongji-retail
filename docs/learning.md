## backend01
## backend02

### 1. Middleware

本项目使用 ASP.NET Core Middleware 统一处理 API 请求过程中的异常。核心实现位于 `backend/src/Middleware/ApiExceptionMiddleware.cs`。

Middleware 可以理解为 ASP.NET Core 请求处理流程中的一层，可以在请求进入 Endpoint 前后执行一些通用逻辑。本项目通过 Middleware 集中处理异常，避免每个 Endpoint 都重复编写相同的异常处理代码。

通过阅读 `ApiExceptionMiddleware`，我了解到项目根据不同异常类型返回不同的 HTTP 状态码：

- `BadHttpRequestException` 和 `JsonException` 返回 400；
- `UnauthorizedAccessException` 返回 401；
- 数据库并发、唯一约束等冲突返回 409；
- 其他未预期异常返回 500。

这种方式可以统一 API 的错误响应格式，也方便前端根据 HTTP 状态码判断错误类型。

### 2. JWT 身份认证

项目使用 JWT Bearer 作为身份认证方式，相关配置主要位于 `Program.cs`。

JWT 的基本使用流程如下：

1. 用户登录；
2. 后端验证用户提交的登录信息；
3. 验证成功后生成 JWT；
4. 客户端保存 Token；
5. 后续访问需要登录的接口时携带 JWT；
6. ASP.NET Core 验证 Token 的签名、Issuer、Audience 和有效期；
7. 验证成功后，将 Token 中的信息转换为当前用户的 Claims；
8. Endpoint 根据用户身份和角色继续执行操作。

项目中的 JWT 密钥可以通过配置文件或环境变量提供，并且程序会检查密钥长度是否满足要求。

### 3. Authentication 与 Authorization

通过阅读 `Program.cs` 和相关 Endpoint，我理解了 Authentication 和 Authorization 的区别。

Authentication 主要解决“用户是谁”的问题。项目通过 JWT Token 对用户身份进行验证。

Authorization 解决的是“这个用户是否有权限执行当前操作”的问题。在身份认证成功之后，系统还会根据用户的角色判断是否允许访问对应接口。

项目中存在 Customer、Merchant、Admin 和 CustomerService 等角色，不同角色可以执行的操作不同。

因此，用户完成登录并不代表可以访问所有接口，还需要通过授权检查。

### 4. JWT Claims 与角色权限

JWT 中可以包含用户身份、角色等 Claims。

项目在 JWT 配置中设置了角色 Claim，使 ASP.NET Core 可以根据 Token 中的角色信息进行授权判断。

例如商家相关接口需要 Merchant 角色，管理员相关接口需要 Admin 角色。通过角色限制接口访问范围，可以形成基于角色的访问控制（RBAC）。

这种方式能够把身份认证和业务权限控制结合起来，同时避免在每个业务函数中重复判断用户角色。

### 5. 401 与 403

通过阅读项目中的 JWT 配置，我进一步理解了 HTTP 401 和 403 的区别。

- 401：身份认证失败，例如没有提供有效 Token，或者 Token 已经失效；
- 403：身份已经认证成功，但是当前用户没有访问该资源的权限。

因此，401 主要对应“你是谁无法确认”，403 对应“已经知道你是谁，但是你没有权限”。

项目在 JWT 配置中分别处理了认证失败和授权失败的情况，并返回对应的 HTTP 状态码。

### 6. 测试代码

项目的 `backend/test` 目录包含认证、订单、商品、客服工单以及 OpenAPI 等相关测试。

通过阅读测试代码，我了解到后端测试不仅需要测试正常业务流程，还需要测试异常输入、身份认证、角色权限以及不同业务状态下的行为。

例如认证测试可以验证用户登录和身份信息获取；订单测试可以验证订单流程以及不同角色执行操作时的权限限制。

项目测试通过 HTTP 请求访问 API，因此测试过程与实际客户端调用接口的方式比较接近，可以用于验证后端接口的整体行为。

### 7. OAuth2 与 JWT

通过本次学习，我也对 OAuth2 和 JWT 的关系有了基本认识。

JWT 是一种 Token 的格式和传递方式，而 OAuth2 是一套授权框架，两者并不是同一个概念。

本项目实际使用的是 JWT Bearer Authentication，并没有自行实现完整的 OAuth2 Authorization Server。因此本项目中主要需要理解 JWT 身份认证、Claims 和角色授权的工作流程。

### 8. 学习总结

通过阅读 `ApiExceptionMiddleware`、`Program.cs`、`JwtService`、认证相关 Endpoint 以及测试代码，我基本理解了一个 API 请求从进入 ASP.NET Core，到异常处理、身份认证、权限判断，再进入具体 Endpoint 的过程。

本次学习主要掌握了 Middleware、JWT、Claims、Authentication、Authorization 和 RBAC 之间的关系，同时了解了后端测试如何验证不同身份、不同权限以及不同业务状态下的接口行为。
## backend03
## frontend01
## frontend02
## frontend03
