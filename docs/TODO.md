# 交付清单与进度

本文件是唯一进度记录。`[x]` 表示完成并验证，`[ ]` 表示尚未完成；进行中或受阻仍保持未勾选，并在交接区说明。每个任务组的全部必做项和验收通过后，该任务组才算完成。

## 当前交接

- 最近更新：2026-09-25。
- 当前阶段：**T01、T02.1 已完成，T02 整体未完成**；六大阶段总览见 [实施计划](implementation-plan.zh-CN.md)。
- 当前代码任务：**T02.2a 已完成**；修改数量命令/处理器、结果 DTO、仓储契约、应用层版本预检查及 MediatR 注册已通过测试替身和实际进程内分发验证。下一小步为 T02.2b 查询报价，尚未实现。
- 实施约定：按 [CONTEXT.md](CONTEXT.md) 每次只推进一小步，先讲设计再实现；讨论问题时不自动写业务代码；理由记录在架构决策第 8 节。
- 架构约定：Sales 保留 Clean 四层；Compositions 在 T04.3 / T05 明确采用六边形端口与适配器，并与 Sales 对照说明；Library、Budget 延续简单分层。六大阶段写入实施计划，完成状态只在本清单维护。
- 本次验证（2026-09-25，修改数量用例）：更新依赖锁文件后 locked-mode 还原通过；编译 0 警告/0 错误；108 项领域、18 项应用（含 MediatR Send 分发）、2 项宿主测试全部通过，共 128 项。仓储使用测试替身；未运行数据库/消息集成检查，未启动 Docker 或独立 API 进程。
- 历史验证（2026-09-18）：实际 HTTP `/health` 返回 200 Healthy；三个容器健康；PostgreSQL Sales schema 可读写且隔离；SQL Server 基准可读且 UPDATE 被拒绝；RabbitMQ 管理接口认证成功；预算初始化可重复执行。2026-09-24 会话中只读检查 `docker compose ps -a` 时 Docker Linux 引擎不可连接；历史健康状态不代表当前可用。
- 下一步：T02.2b，先对照命令解释查询，再实现 GetQuoteQuery/Handler 和只读结果 DTO，返回报价/项目身份、行、金额和版本；复用当前仓储读取入口，用测试替身验证读取、不存在及查询不修改/保存。仍不接 HTTP 或数据库；查询完成后安排 T02.3 的 EF Core/PostgreSQL 持久化与原子版本比较，再接 API。本期预置预算项目标识为 `11111111-1111-1111-1111-111111111111`。
- 环境：Windows；SDK 8.0.425 / 运行时 8.0.31 已在用户目录单独安装；原 SDK 10.0.300 保留；Node 22.22.0、npm 10.9.4；Docker 29.1.3、Compose 2.40.3。
- 阻塞点：当前应用用例与替身验证无阻塞。新 PowerShell 会话先执行 `. ./scripts/Use-Dotnet.ps1` 选择 SDK；后续需要基础设施时再启动并检查 Docker Desktop。
- 已知限制：仓储只有接口和测试替身；尚无持久化版本恢复、数据库原子比较、HTTP 冲突响应或 API 的应用层装配。处理器的版本预检查不能防止读取后发生的并发写入；保存失败后须丢弃该工作单元，不复用已修改的内存报价。数量/售价更新替换只读行实例，EF Core 跟踪映射待设计。项目存在性、每项目一份草稿及配方单位匹配未验证；行定位须携带 QuoteId。Money 仅支持 EUR。业务 API、持久化、其他三个宿主、React 页面和服务间消息尚未实现。应用 Dockerfile 在 T09；React 在 T03。

## 已完成准备

- [x] P01 确定两周范围与明确排除项。
- [x] P02 记录四服务边界、两数据库归属、业务公式和事件流。
- [x] P03 建立任务清单、验收条件与后续接续规则。

## 第一周：报价与跨服务重算

### T01 环境与骨架（第 1 天）

- [x] T01.1 检查 .NET 8 SDK、Node、Docker；锁定已引入依赖/镜像版本，记录选择依据；业务包随相应任务引入。
- [x] T01.2 创建 solution、Sales 宿主、必要分层和测试项目；提供健康检查。
- [x] T01.3 配置 PostgreSQL、SQL Server、RabbitMQ 的 Compose、持久卷、配置示例和独立初始化脚本。

验收：基础设施健康可连接，Sales 启动并返回健康结果；记录实际命令。缺少某工具时说明阻塞并继续不依赖它的实现。

验证记录：`dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore` 均通过，测试 2/2；`./scripts/Test-Infrastructure.ps1` 全部通过；`docker compose ps` 三个服务为 healthy。启动命令见 README。

### T02 报价模型与 API（第 2 天）

- [x] T02.1 实现 Quote、QuoteLine、Money、Quantity 及数量/金额约束，验证行金额与边界规则。
- [x] T02.1a 最小步骤：实现 Quantity 正数值对象与独立领域测试；不接入 API、数据库或消息。
- [x] T02.1b 实现 Money：金额/币种、值相等、显式舍入与边界测试；不提前限制所有金额的正负。
- [x] T02.1c 实现最小 QuoteLine：行身份、工程项/单位、数量、非负售价、行金额及边界测试；修改入口留到 Quote 聚合。
- [x] T02.1d 实现最小 Quote：报价/项目身份、内部创建行、行 ID 去重、只读集合及逐行舍入后的总额；失败不改变状态。
- [x] T02.1e 实现 ChangeLineQuantity：保留行身份和其他字段，重算金额；验证未找到/空值/溢出、跨报价隔离和同值更新。
- [x] T02.1f 实现 ChangeLineSalesUnitPrice：非负售价、保留数量/身份、显式舍入与失败保护；验证同值和金额未变但单价变化的情况。
- [x] T02.1g 实现 RemoveLine：按当前行金额更新总额，允许空草稿；验证缺失/空 ID、重复删除、修改后删除和跨报价隔离。
- [ ] T02.2 实现创建项目/报价、添加/删除行、更新数量/售价及查询的 MediatR 用例和 REST API。
- [x] T02.2a 实现修改数量的 MediatR 命令/处理器与最小仓储契约，使用测试替身验证应用协调；API 与数据库适配器后续实现。
- [ ] T02.2b 实现 GetQuoteQuery/Handler 与只读结果 DTO；验证查询不修改状态或保存，暂不接 HTTP/数据库。
- [ ] T02.3 接入 EF Core/PostgreSQL、迁移和报价版本；验证保存、重新读取、非法输入与并发冲突。
- [x] T02.3a 先实现 Quote.Version 的内存变化规则：有效输入变化递增，同值无操作和失败不递增；数据库原子比较与冲突响应随后接入。

验收：API 完成一次报价编辑流程，重启后能读取；领域规则测试和必要持久化检查通过。

T02.1a 验证（2026-09-24）：`. ./scripts/Use-Dotnet.ps1` 后执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore`；还原通过，编译 0 警告/0 错误，领域 8/8、宿主 2/2。领域测试项目位于 `tests/Unit/ConstructionBudgeting.Sales.Domain.Tests`，仅引用 Domain；T02.1 整体仍未完成。

T02.1b 验证（2026-09-24）：执行上述 SDK、locked-mode 还原、build、test 命令全部通过；编译 0 警告/0 错误，领域 26/26、宿主 2/2。`MoneyTests.cs` 新增 18 个案例，包括零/负数、非法币种、正负舍入中点、原值不变和值相等；1.005 单价 × 100 数量在最终舍入后为 100.50。未新增依赖或持久化，T02.1 整体仍未完成。

T02.1c 验证（2026-09-24）：`. ./scripts/Use-Dotnet.ps1` 后执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore` 全部通过；编译 0 警告/0 错误，领域 43/43、宿主 2/2。新增 `QuoteLineTests.cs` 17 个案例，覆盖 100 × 20 = 2000、12.5 × 19.99 = 249.88、零售价、负售价、空身份/字段/值对象及溢出。未新增包或基础设施，T02.1 整体仍未完成。

T02.1d 验证（2026-09-25）：执行上述 SDK、locked-mode 还原、build、test 命令全部通过；编译 0 警告/0 错误，领域 55/55、宿主 2/2。新增 `QuoteTests.cs` 12 个案例，覆盖空报价、身份、添加汇总、重复行、逐行舍入、多报价隔离、集合保护与失败状态不变。未新增依赖或运行基础设施；编辑规则尚未实现，T02.1 保持未完成。

T02.1e 验证（2026-09-25）：`. ./scripts/Use-Dotnet.ps1` 后执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore` 全部通过；编译 0 警告/0 错误，领域 66/66、宿主 2/2。新增 `QuoteQuantityTests.cs` 11 个案例，验证增减数量、保留身份/售价、舍入、同值无操作、缺失行/空值、溢出失败保护及跨报价隔离。未运行基础设施检查。

T02.1f 验证（2026-09-25）：`. ./scripts/Use-Dotnet.ps1` 后执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore` 全部通过；编译 0 警告/0 错误，领域 82/82、宿主 2/2。新增 `QuoteSalesPriceTests.cs` 16 个案例，覆盖涨价/降价/零价、负价、精度与舍入、同值、金额不变但单价变化、空值/缺失行、溢出、跨报价隔离及与数量修改交错。未运行基础设施检查。

T02.1g 验证（2026-09-25）：`. ./scripts/Use-Dotnet.ps1` 后执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore` 全部通过；编译 0 警告/0 错误，领域 92/92、宿主 2/2。新增 `QuoteRemovalTests.cs` 10 个案例，覆盖剩余行顺序/身份、空草稿可继续添加、零价行、空/缺失 ID、重复删除、修改后最新金额、舍入、跨报价隔离及删除后不可编辑。T02.1 的模型与编辑规则完成，T02 的 API/持久化验收尚未完成；未运行基础设施检查。

T02.3a 验证（2026-09-25）：`. ./scripts/Use-Dotnet.ps1` 后执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore` 全部通过；编译 0 警告/0 错误，领域 108/108、宿主 2/2。新增 `QuoteVersionTests.cs` 16 个案例，验证初始值、四种编辑递增、空报价不重置版本、同值无操作、零金额/舍入金额不变的有效修改、校验/金额溢出失败、重复删除、改回原值及报价间独立。未运行数据库或消息检查；T02.3 的持久化与并发验收尚未完成。

T02.2a 验证（2026-09-25）：`. ./scripts/Use-Dotnet.ps1` 后运行 `dotnet restore ConstructionBudgeting.sln` 更新新依赖锁文件，再执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore`，全部通过；编译 0 警告/0 错误，领域 108/108、应用 18/18、宿主 2/2。新增应用测试项目 `tests/Unit/ConstructionBudgeting.Sales.Application.Tests`，验证成功返回领域金额、保存原版本、零价有效修改、同值不保存、过期版本（含同值请求）、非法输入、缺失报价/行、计算溢出、保存冲突/故障传播、取消及 DI/MediatR 分发。数据库原子性、真实存储、HTTP 和 RabbitMQ 尚未验证。

### T03 最小报价页面（第 3 天）

- [ ] T03.1 创建 React/TypeScript/React Query 应用，项目选择与报价工作台。
- [ ] T03.2 实现行编辑/删除、保存、加载和错误提示；显示服务器返回金额。
- [ ] T03.3 在浏览器完成创建、修改、刷新恢复，记录操作结果。

验收：页面可使用真实 API 操作报价，无前端假数据替代持久化流程。

### T04 资源库与跨服务消息（第 4 天）

- [ ] T04.1 建 Library 独立宿主、资源表和种子数据，提供资源成本价编辑 API 与简单价格表界面。
- [ ] T04.2 接入 MassTransit/RabbitMQ，定义并发布 ResourcePriceChanged 与 QuoteInputsChanged 契约。
- [ ] T04.3 先对照 Sales 解释六边形边界，再建 Compositions 独立宿主、核心输入/输出端口及消息/持久化适配器；消费上述事件并持久化输入，携带消息标识、关联标识和版本。

验收：修改一条资源价格或报价数量，另一个进程接收事件并记录正确输入；检查 Compositions 的适配器依赖核心约定，核心不引用适配器实现，领域规则不依赖 EF Core 或 MassTransit 类型。

### T05 成本组成重算（第 5 天）

- [ ] T05.1 初始化刷漆/地板配方，关联资源和报价行，完成数量 × 消耗系数 × 成本单价计算。
- [ ] T05.2 持久化成本快照和计算修订号，发布 CompositionCosted；处理重复输入和旧资源/报价版本。
- [ ] T05.3 通过输入端口直接驱动用例，以内存或测试替身实现输出端口，验证数量变化、资源改价、销售价变化和删除报价行；检查成本变化及不应变化的情况，并单独验证真实适配器集成。

验收：两条变更路径产生正确成本，重复事件不重复累加；关键计算测试通过。

## 第二周：预算、协作与交付

### T06 预算与毛利闭环（第 6 天）

- [ ] T06.1 建 Budget 独立宿主和 SQL Server 只读适配器；初始化预算基准，确认应用账号写入被拒绝。
- [ ] T06.2 消费成本结果，保存预算差额，发布 BudgetImpactCalculated；Sales 保存毛利与最终报价视图。
- [ ] T06.3 页面显示成本明细、预算、差额、毛利、毛利率和来源版本；处理未配置基准与计算中状态。
- [ ] T06.4 对照架构文档四行固定金额样例验证全链路，检查结果应用不触发事件循环。

验收：修改报价和修改资源两条路径均更新最终页面，SQL Server 基准不被改变。

### T07 实时更新与并发（第 7 天）

- [ ] T07.1 Sales 接入 SignalR，按项目分组发送受影响报价通知，React Query 定向重新查询。
- [ ] T07.2 连接恢复后重新入组、补读状态；页面提示断线和编辑冲突，计算中可短期轮询兜底。
- [ ] T07.3 两窗口验证联动、同时保存冲突、断线错过通知后恢复。

验收：两个页面最终看到同一最新结果，同时编辑不会静默覆盖。

### T08 关键异常验证（第 8 天）

- [ ] T08.1 验证消费者持久化去重与原子版本保护，输入交错或旧结果晚到不会回滚金额。
- [ ] T08.2 配置有限重试和可定位错误记录/队列；显示失败或长期未完成状态，提供按最新输入重新触发计算的操作。
- [ ] T08.3 验证重复消息、停止/恢复消费者、预算库不可用后重算；记录仍然存在的投递限制。

验收：结果不重复累加、不被旧结果覆盖；失败可定位且存在经过验证的恢复路径，不宣称尚未实现的自动恢复能力。

### T09 完整 Docker Compose（第 9 天）

- [ ] T09.1 为四个后端服务和前端配置 Dockerfile，与数据库/消息服务整合；应用端口限定本机访问。
- [ ] T09.2 配置启动依赖、健康检查、迁移/种子执行方式和持久化卷；配置不包含真实凭据。
- [ ] T09.3 从干净的项目数据环境启动整套流程，验证服务重启后数据保留，记录真实启动/停止命令。

验收：本机按文档启动完整系统；重置数据只能操作明确属于本项目的资源。

### T10 验收与文档（第 10 天）

- [ ] T10.1 运行关键计算、持久化、消息集成检查，完成两条业务路径与两窗口浏览器验收。
- [ ] T10.2 修复阻断流程的问题，记录未完成项和已知限制。
- [ ] T10.3 更新 README 的实际能力、环境要求、启动方式和操作步骤；更新本清单与交接区。

验收：从启动到报价、改价、重算、通知的全过程可复现，有对应检查记录。

## 可选增强（不阻塞 T01–T10）

- [ ] O01 事务性 Outbox：业务状态与待发消息同事务；验证提交后进程中断再恢复发布，覆盖下游消费者发布。
- [ ] O02 补充更多金额边界、资源扇出和错误恢复的自动化检查。
- [ ] O03 在不新增页面或服务的前提下完善表格可读性和操作反馈。

Kubernetes、云、CI/CD、鉴权、Redis、多实例、自动定价与完整端到端测试平台不列入本期候选任务。

## 工作记录

每次完成一段开发后追加一行，并同时更新顶部交接区。记录有用的文件路径、验证命令/结果和下一步；未执行的检查写明未执行。

| 日期 | 任务 | 修改与验证 | 接续动作 |
| --- | --- | --- | --- |
| 2026-09-18 | P01–P03 | 收敛交付范围，更新 README、实施计划和架构决策，建立 TODO 与 AGENTS；校对文档链接和金额样例；未运行应用测试 | T01：环境检查与 Sales 骨架 |
| 2026-09-18 | T01.1–T01.3 | 新增 solution、Sales 四层、宿主集成测试、Compose、初始化/验证脚本、版本锁定；build 0 警告，test 2/2，HTTP 200，三个容器与账号隔离检查通过 | T02.1：报价领域模型与规则测试 |
| 2026-09-18 | 设计说明 | 补充服务划分、四层依赖、DDD 聚合、命令查询与事件通信的理由及代价；本次仅文档变更 | T02.1：先说明聚合边界与业务不变量，再实现模型 |
| 2026-09-18 | 上下文接续 | 新增 CONTEXT，记录小步推进、解释优先、CQRS 范围与排除项；AGENTS 设置接续入口，TODO 校正下一步；本次未改业务代码 | 先介绍已有 Sales 骨架和引用关系 |
| 2026-09-19 | 架构分工约定 | 更新 docs/CONTEXT.md、docs/architecture-decisions.zh-CN.md、docs/implementation-plan.zh-CN.md、README.md 及本清单；记录 Sales Clean / Compositions 六边形的理由、边界、取舍和 T04.3 / T05 验收；git diff --check 通过，未运行应用测试，业务任务未勾选 | 先介绍 Sales 骨架，再进入 T02.1 的最小模型；Compositions 按后续顺序实现 |
| 2026-09-24 | T02.1a / 六阶段总览 | 新增 Sales.Domain/Quotations/Quantity.cs 与 tests/Unit/ConstructionBudgeting.Sales.Domain.Tests，加入 solution 和锁文件；实施计划记录六阶段与 T02–T10 映射；更新架构理由及 README。locked-mode 还原通过，build 0 警告/0 错误，test 8 项领域 + 2 项宿主通过；未执行基础设施集成检查 | 先回顾 Quantity 实现，再解释并实现 Money；T02.1 保持未完成 |
| 2026-09-24 | T02.1b | 新增 Sales.Domain/Quotations/Money.cs 与领域测试 MoneyTests.cs；更新架构理由及 README。locked-mode 还原通过，build 0 警告/0 错误，领域 26 + 宿主 2 项测试通过；未执行数据库/消息集成检查 | 回顾 Money，再讲解并实现最小 QuoteLine；不同时推进 Quote、API 或持久化 |
| 2026-09-24 | T02.1c | 新增 Sales.Domain/Quotations/QuoteLine.cs 与领域测试 QuoteLineTests.cs；更新架构理由及 README。locked-mode 还原通过，build 0 警告/0 错误，领域 43 + 宿主 2 项测试通过；未执行数据库/消息集成检查 | 回顾 QuoteLine，再讲解 Quote 聚合，先做创建/添加行/汇总；暂不推进 API 或持久化 |
| 2026-09-25 | T02.1d | 新增 Sales.Domain/Quotations/Quote.cs 与领域测试 QuoteTests.cs；更新聚合边界、行身份范围及 README。locked-mode 还原通过，build 0 警告/0 错误，领域 55 + 宿主 2 项测试通过；未执行数据库/消息集成检查 | 回顾 Quote 后，通过聚合修改一行数量并验证总额与失败状态；售价、删除、版本继续拆小 |
| 2026-09-25 | T02.1e | Quote.cs 新增 ChangeLineQuantity，新增领域测试 QuoteQuantityTests.cs；更新只读行替换取舍及 README。locked-mode 还原通过，build 0 警告/0 错误，领域 66 + 宿主 2 项测试通过；未运行基础设施检查 | 通过 Quote 修改销售单价；随后再分步处理删除和版本 |
| 2026-09-25 | T02.1f | Quote.cs 新增 ChangeLineSalesUnitPrice，新增领域测试 QuoteSalesPriceTests.cs；更新售价约束及 README。locked-mode 还原通过，build 0 警告/0 错误，领域 82 + 宿主 2 项测试通过；未运行基础设施检查 | 通过 Quote 删除行并维护总额；版本另起小步 |
| 2026-09-25 | T02.1g | Sales.Domain/Quotations/Quote.cs 新增 RemoveLine；tests/Unit/ConstructionBudgeting.Sales.Domain.Tests/QuoteRemovalTests.cs 新增 10 项测试，更新架构决策及 README。上述 locked-mode 还原、build、test 通过，0 警告/0 错误，领域 92 + 宿主 2 项通过；未运行基础设施检查 | T02.3a 报价版本的内存规则，再进入 T02.2 用例 |
| 2026-09-25 | T02.3a | Sales.Domain/Quotations/Quote.cs 增加 Version 及四种编辑的递增规则；tests/Unit/ConstructionBudgeting.Sales.Domain.Tests/QuoteVersionTests.cs 新增 16 项测试，更新架构决策及 README。上述 locked-mode 还原、build、test 通过，0 警告/0 错误，领域 108 + 宿主 2 项通过；未运行基础设施检查 | T02.2a 修改数量的应用命令/处理器与仓储契约 |
| 2026-09-25 | T02.2a | Sales.Application/Quotations/ChangeQuoteLineQuantity 新增命令、处理器、结果；新增 IQuoteRepository、QuoteConcurrencyException、DependencyInjection.cs，固定 MediatR 12.5.0，加入应用测试项目及锁文件。上述还原/build/test 通过，领域 108 + 应用 18 + 宿主 2；未运行基础设施检查 | T02.2b 查询报价用例，返回 DTO 并验证只读行为 |
