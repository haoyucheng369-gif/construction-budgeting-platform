# 交付清单与进度

本文件是唯一进度记录。`[x]` 表示完成并验证，`[ ]` 表示尚未完成；进行中或受阻仍保持未勾选，并在交接区说明。每个任务组的全部必做项和验收通过后，该任务组才算完成。

## 当前交接

- 最近更新：2026-09-25。
- 当前阶段：**T01、T02.1 已完成，T02 整体未完成**；六大阶段总览见 [实施计划](implementation-plan.zh-CN.md)。
- 当前代码任务：**T02.2c 已完成并验证**；T02.3c 与中文注释已推送（3d957c5）。按用户要求接入查询/修改数量 HTTP、Swagger UI、显式示例初始化，并修复 Windows PowerShell 5.1 中文脚本编码问题。
- 当前说明整理：按用户反馈，将 EfQuoteRepository 的长注释精简为 6 条短中文说明；CONTEXT 记录“源码少量短注释，详细原理在对话中解释”的偏好。已核对非注释内容不变，仅修改注释和文档，未重跑测试；该整理已包含在 3d957c5。
- 本次整理：现有源码、测试、脚本及配置示例中的英文说明注释已统一翻译为中文。保留 XML 注释标签、自动生成标记及脚本解释器声明；对 48 个源码/脚本/配置文件核对非注释内容，修改前后一致，`git diff --check` 通过。此注释批次已包含在 3d957c5；随后 HTTP 验收发现并修复 PowerShell 中文编码问题，见本次验证。
- 实施约定：按 [CONTEXT.md](CONTEXT.md) 每次只推进一小步，先讲设计再实现；讨论问题时不自动写业务代码；理由记录在架构决策第 8 节。
- 架构约定：Sales 保留 Clean 四层；Compositions 在 T04.3 / T05 明确采用六边形端口与适配器，并与 Sales 对照说明；Library、Budget 延续简单分层。六大阶段写入实施计划，完成状态只在本清单维护。
- 本次验证（2026-09-25，HTTP/Swagger）：./scripts/Test-SalesPersistence.ps1 的 locked-mode 还原/build/test 通过，0 警告/0 错误，154 项通过、无跳过（领域 108、应用 27、持久化 12、API 7；共 14 项真实 PostgreSQL）。最初因 Windows PowerShell 5.1 误读无 BOM 中文脚本导致连接缺失密码，修正 scripts/*.ps1 为 UTF-8 BOM 后重跑通过。示例初始化连续两次成功，未覆盖数据；实际 Kestrel 的 /health、/swagger/index.html、Swagger JSON、报价 GET 均返回 200，同值 PATCH 保持版本、旧版本 PATCH 返回 409。浏览器自动化不可用，未代点击 Swagger UI；页面 HTML 与接口已通过 HTTP 验证。
- 历史验证（2026-09-18）：实际 HTTP `/health` 返回 200 Healthy；三个容器健康；PostgreSQL Sales schema 可读写且隔离；SQL Server 基准可读且 UPDATE 被拒绝；RabbitMQ 管理接口认证成功；预算初始化可重复执行。2026-09-24 会话中只读检查 `docker compose ps -a` 时 Docker Linux 引擎不可连接；历史健康状态不代表当前可用。
- 下一步：先让用户按 docs/sales-api-walkthrough.zh-CN.md 在 Swagger 执行 GET → PATCH → GET，并观察 pgAdmin。下一业务任务 T02.2d：解释并实现创建报价的最小用例与 HTTP 入口；创建时项目存在性如何验证需单独明确，不能把示例初始化当成已完成创建项目功能。其余行编辑、前端和消息继续拆步。
- 当前本机运行：Sales 开发宿主本次已启动在 http://127.0.0.1:5080，Swagger 在 /swagger；示例报价 22222222-2222-2222-2222-222222222222，油漆行 33333333-3333-3333-3333-333333333333。交接时油漆数量 100、总额 2049.98、版本 3；用户操作后以 GET 为准。接续时重新检查进程，完整启动命令见手动指南。
- 环境：Windows；SDK 8.0.425 / 运行时 8.0.31 已在用户目录单独安装；原 SDK 10.0.300 保留；Node 22.22.0、npm 10.9.4；Docker 29.1.3、Compose 2.40.3。
- 本机数据库查看工具：用户已安装 Windows 桌面版 pgAdmin，连接 `127.0.0.1:5432`、数据库 `vente`、账号 `sales_app`，密码取本机 `.env`。按用户要求已删除旧网页工具容器 `construction-pgadmin`、专属设置卷 `construction-pgadmin-data` 和镜像 `dpage/pgadmin4:9.18`；不再使用 5050 网页入口。项目 PostgreSQL 及其 `construction-budgeting_postgres-data` 数据卷保留，已启动并验证 healthy、主机 5432 端口可达、sales_app TCP 登录及三张 sales 表存在。用户随后提供的截图已显示桌面 pgAdmin 连接 vente，并执行 Quotes 查询返回列结构；原网页工具宿主机配置目录仍保留但不再使用。
- 本机凭据交接（2026-09-25）：用户修改 `.env` 后，已通过 ALTER ROLE 同步 PostgreSQL 五个账号，保留原数据卷，重新应用 PostgreSQL 容器配置并同步 pgAdmin 的宿主机密码文件及卷内 `.pgpass`。五账号 TCP 登录、pgAdmin 保存凭据查询 sales 表和 HTTP 200 均通过。SQL Server/RabbitMQ 仍停止，尚未同步其实际账号密码；当前 `.env` 的 SQL Server 管理员密码不满足复杂度要求，启用这两项服务前须单独处理，不能仅重启或假定所有凭据已同步。文档不记录密码值。
- 桌面 pgAdmin 9.18 连接兼容性：用户遇到空指针 access violation，与上游 Windows GSSAPI 打包问题 #10428 相符。使用安装包自带 Python/psycopg，加入运行库查找路径后，以 `gssencmode=disable` 连接主机 `127.0.0.1:5432/vente`，查询返回 `sales_app / vente`，进程退出码 0。已提供在 GUI 的 Connection Parameters 中添加 `GSS encmode = disable` 的办法；用户后续截图显示界面连接与 Quotes 查询成功，不改数据库认证或 SSL 配置。
- 阻塞点：当前无阻塞；本次 PostgreSQL 容器 healthy，监听 127.0.0.1:5432，其他两个容器仍停止。接续时重新检查状态。新会话执行 `. ./scripts/Use-Dotnet.ps1`；EF CLI 前另执行 `. ./scripts/Use-SalesDatabase.ps1`，密码不写入文档。
- 已知限制：EfQuoteRepository 的每次读写使用独立上下文，保存仅支持已有且有效修改的单份报价，整份行快照在事务内替换，写入量与行数成正比；大报价、外部行引用及多聚合事务需重新评估。并发仍以整份报价为单位，失败后须丢弃内存聚合。每项目一份报价的数据库唯一约束已验证，项目存在性/配方单位匹配仍未验证。numeric 保留 decimal 精度，任意外部 SQL 写入的超范围/不一致数据不受完整领域保护。已装配 API DI 并提供报价 GET/数量 PATCH；创建报价、添加/删除行、改售价的 HTTP、其他三个宿主、React 与消息尚未实现。示例初始化是显式本地命令，不是完整创建用例；应用 Dockerfile 在 T09。

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
- [x] T02.2b 实现 GetQuoteQuery/Handler 与只读结果 DTO；验证查询不修改状态或保存，暂不接 HTTP/数据库。
- [x] T02.2c 装配 API 应用/数据库依赖，提供报价 GET 与数量 PATCH（用户本次明确扩展）、Swagger UI 及显式示例初始化；验证真实数据、400/404/409、健康检查与初始化可重复执行。
- [ ] T02.2d 实现最小创建报价用例与 HTTP 入口，明确项目存在性校验边界；添加行与其他编辑接口继续拆小步。
- [x] T02.3 接入 EF Core/PostgreSQL、迁移和报价版本；验证保存、重新读取、非法输入与并发冲突。
- [x] T02.3a 先实现 Quote.Version 的内存变化规则：有效输入变化递增，同值无操作和失败不递增；数据库原子比较与冲突响应随后接入。
- [x] T02.3b 实现最小 SalesDbContext、聚合映射与初始迁移；验证 PostgreSQL 保存后在新上下文读回身份、顺序、值对象、金额及版本。仓储适配器和并发保存另起小步。
- [x] T02.3c 实现 EF/PostgreSQL IQuoteRepository，按原版本原子保存聚合，处理只读行替换与顺序；真实数据库验证成功保存、冲突与事务完整性。

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

T02.2b 验证（2026-09-25）：`. ./scripts/Use-Dotnet.ps1` 后执行 `dotnet restore ConstructionBudgeting.sln --locked-mode`、`dotnet build ConstructionBudgeting.sln --no-restore`、`dotnet test ConstructionBudgeting.sln --no-build --no-restore` 全部通过；编译 0 警告/0 错误，领域 108/108、应用 27/27、宿主 2/2。新增 `GetQuoteTests.cs` 9 个案例，覆盖身份/行字段/顺序/舍入金额、空报价、不改变聚合或保存、独立只读快照、缺失报价、空 ID/null、取消、读取故障传播，以及 MediatR 查询→修改→查询。未新增依赖，未执行 HTTP 业务、数据库或消息验收。

T02.3b 验证（2026-09-25）：`docker desktop start`、`docker compose up -d --wait --wait-timeout 60 postgres` 成功，容器 healthy。更新新依赖锁文件后，`./scripts/Test-SalesPersistence.ps1` 执行 locked-mode 还原、build、test，全套 143/143 通过。新增 `tests/Integration/ConstructionBudgeting.Sales.Persistence.Tests`：迁移快照检查 1 项、真实 PostgreSQL 5 项，覆盖多行身份/顺序/编辑后版本与金额、空草稿、精细小数及 decimal.MaxValue、跨报价重复行 ID、项目唯一约束、数据库拒绝零数量；随机测试报价在 finally 中按 ID 清理，未删除库或 schema。`dotnet tool restore`、`dotnet ef database update --project src/Services/Sales/ConstructionBudgeting.Sales.Infrastructure --no-build` 通过（先加载 SDK 与 Use-SalesDatabase）；重复更新无待执行迁移。未设置 SALES_PERSISTENCE_TESTS 时，该测试项目 1 项通过、5 项跳过，非数据库验收通过。

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
| 2026-09-25 | T02.2b | Sales.Application/Quotations/GetQuote 新增 Query、Handler、GetQuoteResult、QuoteLineDetails；应用测试 GetQuoteTests.cs 新增 9 项，更新架构决策及 README。上述 locked-mode 还原/build/test 通过，领域 108 + 应用 27 + 宿主 2；未运行基础设施检查 | T02.3b 最小 EF Core 映射与迁移，验证真实数据库保存/读回 |
| 2026-09-25 | T02.3b | Sales.Infrastructure/Persistence 新增上下文、配置、设计时工厂及 InitialSales 迁移；新增本地 EF 工具、Use-SalesDatabase/Test-SalesPersistence 脚本、持久化测试项目和锁文件。真实 PostgreSQL 验证与全套测试 143 项通过，build 0 警告/0 错误；迁移重复执行通过，未测试业务 HTTP/消息 | T02.3c 仓储适配器与整份报价原子并发保存 |
| 2026-09-25 | T01.1 本机辅助工具 | 安装官方 dpage/pgadmin4:9.18 容器并预置 Sales 连接，绑定本机 5050 端口；HTTP 200、setup.py dump-servers 和容器内 psql 查询 sales 表通过。原生安装因文件访问错误回滚，浏览器控制不可用，未验证界面操作；未改业务代码或重跑应用测试 | 通过 pgAdmin 查看表；下一代码任务仍为 T02.3c |
| 2026-09-25 | T01.1 本机凭据修复 | 确认 `.env` 与 PostgreSQL 已存凭据不一致；同步五账号及 pgAdmin 密码文件，`docker compose up -d --wait --wait-timeout 60 postgres`、`docker restart construction-pgadmin` 后验证 TCP 登录；`docker exec --user pgadmin -e PGPASSFILE=/var/lib/pgadmin/.pgpass construction-pgadmin /usr/local/pgsql-17/psql -h postgres -U sales_app -d vente -w -c '\dt sales.*'` 通过，网页 HTTP 200。未修改业务代码、未重跑应用测试 | T02.3c；启用 SQL Server/RabbitMQ 前处理各自凭据 |
| 2026-09-25 | T01.1 数据库管理工具切换 | 用户改用桌面 pgAdmin；执行 `docker rm construction-pgadmin`、`docker volume rm construction-pgadmin-data`、`docker image rm dpage/pgadmin4:9.18`；保留项目数据卷。`docker compose up -d --wait --wait-timeout 60 postgres` 通过，5432 可达，sales_app TCP 登录及 information_schema 查询三张 sales 表通过；SQL Server/RabbitMQ 保持停止。更新 README；未改业务代码或重跑应用测试 | 桌面 pgAdmin 直连；下一代码任务仍为 T02.3c |
| 2026-09-25 | T01.1 pgAdmin 连接诊断 | 查证上游 #10428/#10440；通过桌面包 Python/psycopg 设置 `gssencmode=disable` 实测主机 TCP 连接成功，查询身份及数据库通过，退出码 0。README 记录连接参数；未代操作 GUI，未改业务代码或重跑应用测试 | 用户在 Connection Parameters 添加该参数重连；T02.3c 不变 |
| 2026-09-25 | T02.3c | 推送 T02.3b（f0309f7）；新增 Sales.Infrastructure/Persistence/EfQuoteRepository.cs、持久化测试 QuoteRepositoryTests.cs；工厂提供独立上下文，事务内按原版本更新表头并替换行快照。./scripts/Test-SalesPersistence.ps1 的 locked-mode 还原/build/test 全部通过，0 警告/0 错误，149 项通过（11 项真实 PostgreSQL）；更新 README 与架构说明，无新迁移/依赖，未验收业务 HTTP/消息；本步尚未提交 | T02.2c 最小查询 HTTP 接口与依赖装配 |
| 2026-09-25 | T02.3c 注释整理 | EfQuoteRepository.cs 全部注释改为中文并补充关键代码块说明；CONTEXT 记录后续中文注释约定。仅注释/文档调整，人工核对与 git diff --check 通过，未重跑测试，未提交或推送 | T02.2c 最小查询 HTTP 接口与依赖装配 |
| 2026-09-25 | 全库注释中文化 | 翻译 Sales 各层、单元/集成测试、scripts、infra/sqlserver 及 .env.example 的现有英文说明注释；保留工具语法标记。48 个源码/脚本/配置文件的非注释内容核对一致，git diff --check 通过；未重跑业务测试，未提交或推送 | T02.2c 最小查询 HTTP 接口与依赖装配 |
| 2026-09-25 | 仓储注释精简 | EfQuoteRepository 仅保留 6 条关键步骤短注释，CONTEXT 更新简短注释偏好；非注释内容比对一致，git diff --check 通过；未重跑测试，未提交或推送 | 按执行顺序解释两个方法，下一业务任务仍为 T02.2c |

| 2026-09-25 | T02.2c | 推送 T02.3c（3d957c5）；Program.cs 装配 MediatR/EF 仓储与 GET/PATCH，新增 ChangeQuantityRequest、SalesExceptionHandler、SalesSampleData、QuoteHttpTests；固定 Swashbuckle.AspNetCore 9.0.6 及锁文件。修复 .ps1 为 UTF-8 BOM 并添加 .editorconfig。Test-SalesPersistence.ps1 重跑 154 项全通过；CLI --seed-sample 两次成功；实际 HTTP 200/409 和同值保存验证通过。新增手动操作指南；未代点击 UI；提交内容包含 API、Swagger、操作指南与脚本编码修复 | 用户手动验证后，T02.2d 最小创建报价用例 |

| 2026-09-25 | T02.2c 交付整理 | 按用户请求准备提交并推送本批改动；沿用已通过的 154 项测试及 HTTP 验收，业务代码未再修改，git diff --check 通过；实际提交和推送结果以 Git 记录为准 | 下一步 T02.2d：创建报价用例与 HTTP，先解释项目关联和重复报价约束 |
