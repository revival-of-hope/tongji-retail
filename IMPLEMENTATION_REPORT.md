# 第一阶段改进实施报告

## 已完成

- 商城价格筛选、分页、完整树形分类选择，父分类自动包含后代分类商品。
- 管理员工单处理界面与客服工单工作台组件复用。
- 待支付订单读取时自动失效，并由后台服务每分钟清理过期订单。
- 订单商品返回已评价状态，前端阻止重复评价入口。
- 注册、登录、商品、查询、工单、订单和枚举输入验证加强；错误统一为 API Envelope。
- 加购、下单、支付、发货、取消、评价和工单接取的并发/事务边界加强。
- 销售报表改按成功支付时间统计；聚合查询尽量在数据库执行。
- 保持 12 张核心表不变，增加金额、库存、数量、评分和状态检查约束及高频索引。
- OpenAPI 由 operationId 检查升级为完整契约比较；前端生成客户端纳入漂移检查。
- Oracle 冒烟测试升级为注册、加购、下单、支付、发货、收货、评价、工单、库存和报表全链路。
- 增加 GitHub Actions：后端、前端和 Oracle 端到端三个作业。
- 恢复 OpenAPI 客户端文档，增加发布打包脚本和数据库更新说明。

## 验证命令

```bash
sh ./scripts/test-all.sh
docker compose up --build --detach --wait
sh ./scripts/smoke-test.sh
docker compose down --volumes
```

数据库检查约束只会在新数据库中通过 `EnsureCreated` 创建。使用旧 Oracle 数据卷时，应先备份数据，再执行 `docker compose down --volumes` 创建新卷，或自行编写迁移脚本。
