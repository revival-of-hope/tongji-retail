## backend01
## backend02
## backend03
## frontend01
## frontend02
## frontend03

在本项目中，我负责 frontend03 模块，核心任务是利用 Zustand 实现全局状态管理，并通过 Vitest 编写单元测试验证业务逻辑。以下是我在开发过程中的技术总结与思考：

#### 1. 基于 Zustand 的全局状态管理实践
在本项目的前端架构中，状态的跨组件共享是开发重心。我通过 Zustand 实现了一套轻量且响应式的身份验证 Store (`auth.ts`)。

不同于 Redux 的复杂模板，本项目采用了 Zustand 的函数式创建模式。通过定义 `user`（用户信息）和 `hydrated`（持久化状态）两个核心变量，实现了状态的精简控制。

同时考虑到电商平台用户刷新的高频操作，设计出了 `hydrate` 逻辑。通过从 `localStorage` 同步用户信息，确保了用户在页面刷新后依然能保持登录状态，解决了单页应用状态瞬时性的问题。

将 `login`、`register`、`logout` 等异步逻辑封装在 Store 内部。这种方式不仅简化了组件层的代码，更让业务逻辑与 UI 展示实现了彻底解耦。

#### 2. 路由守卫与访问控制逻辑

实现并应用了 `AuthGuard` 组件。其核心逻辑是通过监听 Zustand Store 中的 `user` 和 `hydrated` 状态：
*   利用 `useEffect` 在应用初始化时恢复缓存数据。

*   通过判断用户登录状态及当前路径，实现了非登录用户自动重定向至 `/auth/login`。这种“路由守卫”模式是保障电商平台数据安全的第一道防线。

#### 3. Vitest：独立于环境的逻辑验证
在编写 `tests/auth.test.ts` 的过程中，我掌握了单元测试的核心思想——环境解耦：
*   API Mocking：由于后端环境在本地部署时可能受限（如 Docker 虚拟化问题），我学会了使用 `vi.mock` 和 `vi.fn` 模拟后端接口返回，确保前端逻辑测试不依赖于真实的服务器响应。
*   环境模拟：针对 Node.js 环境缺少浏览器 API 的问题，我通过 `Object.defineProperty` 手动模拟了 `window` 和 `localStorage` 对象。这证明了只要逻辑严谨，前端开发可以实现高度的自给自足。

#### 4. 总结

通过本次项目，我不仅掌握了 Zustand 和 Vitest 的具体语法，还学会了如何在复杂的工程环境下，通过 Mock 方案保持开发节奏，确保模块的独立性和可测试性。更对 Next.js 下的权限控制架构有了全局性的认识。

