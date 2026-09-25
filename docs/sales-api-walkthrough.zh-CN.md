# 报价创建与编辑

Sales 提供六个操作：创建报价、查询报价、添加行、修改数量、修改销售单价、删除行。Swagger UI 已预填示例 ID 和请求体，返回值来自 PostgreSQL。

## 预填参数怎么用

点击 **Try it out** 后，GET 和编辑接口会预填示例报价 `22222222-2222-2222-2222-222222222222`。改数量/售价默认选择墙面涂装行，删除默认选择地板行。请求体已填好数量、单价、工程项和版本 3。

默认值只是 Swagger 表单示例，不是服务器自动补参数。首次初始化后可直接执行；每次成功修改后，使用响应或 GET 中的最新 `version` 替换下一次请求的 `expectedVersion`。保留旧版本会得到 409，这是正常的并发保护。已删除的行要换成仍存在的 `lineId`。

| 操作 | 路径 | 成功响应 |
| --- | --- | --- |
| 创建空报价 | POST /quotes | 201，返回报价 ID、版本 1、零总额及 Location |
| 查询报价 | GET /quotes/{id} | 200，包含全部报价行和当前版本 |
| 添加行 | POST /quotes/{quoteId}/lines | 201，返回新行 ID、新总额和新版本 |
| 修改数量 | PATCH /quotes/{quoteId}/lines/{lineId}/quantity | 200，返回最新金额和版本 |
| 修改售价 | PATCH /quotes/{quoteId}/lines/{lineId}/sales-unit-price | 200，返回最新金额和版本 |
| 删除行 | DELETE /quotes/{quoteId}/lines/{lineId}?expectedVersion=... | 200，返回剩余总额和新版本 |

## 启动

在项目根目录打开 PowerShell，先启动 Docker Desktop，再执行：

```powershell
. ./scripts/Use-Dotnet.ps1
. ./scripts/Use-SalesDatabase.ps1
docker compose up -d --wait --wait-timeout 60 postgres
dotnet restore ConstructionBudgeting.sln --locked-mode
dotnet build ConstructionBudgeting.sln --no-restore
dotnet run --no-build --project src/Services/Sales/ConstructionBudgeting.Sales.Api --launch-profile Sales -- --seed-sample
dotnet run --no-build --project src/Services/Sales/ConstructionBudgeting.Sales.Api --launch-profile Sales
```

`--seed-sample` 显式应用迁移并创建下面的示例报价，完成后退出。重复执行保留已有报价及修改，不重置数量或版本。正常启动不创建数据、不执行迁移；示例身份已被其他报价占用时初始化报错。最后一条命令启动服务，保持这个终端运行，按 Ctrl+C 停止。

打开 [Swagger UI](http://127.0.0.1:5080/swagger/index.html)。Swagger 只在 Development 环境提供，Sales 启动配置已使用该环境。

## 查询报价

展开 `GET /quotes/{id}`，点击 **Try it out**，在 `id` 填入：

```text
22222222-2222-2222-2222-222222222222
```

点击 **Execute**，查看下面的 **Server response → Response body**，不要将静态 Example Value 当成查询结果。

首次初始化时的数据如下：

| 工程项 | 数量 | 销售单价 | 行金额 |
| --- | --- | --- | --- |
| 墙面涂装 | 100 m2 | 20 EUR | 2000 EUR |
| 地板铺设 | 2.5 m2 | 19.99 EUR | 49.98 EUR |

报价 `totalSalesAmount` 为 **2049.98**，`version` 为 **3**。版本从 1 开始，添加两行后成为 3。已经修改过报价时，以实际查询值为准。

## 修改工程量

展开 `PATCH /quotes/{quoteId}/lines/{lineId}/quantity`，点击 **Try it out**。

| 参数 | 值 |
| --- | --- |
| quoteId | `22222222-2222-2222-2222-222222222222` |
| lineId（墙面涂装） | `33333333-3333-3333-3333-333333333333` |

请求体填入以下 JSON；`expectedVersion` 必须使用刚才 GET 返回的版本：

```json
{
  "quantity": 120,
  "expectedVersion": 3
}
```

点击 **Execute**。第一次从 100 改为 120 后，返回 200：`quantity` 为 120，`lineAmount` 为 2400，`totalSalesAmount` 为 **2449.98**，`version` 为 **4**。

再次执行 GET，应读到同样的结果。重启 API 后结果仍保留，因为修改已经写入数据库。

## 查看版本冲突与非法输入

- 保留旧的 `expectedVersion: 3` 再执行 PATCH，会返回 **409**，提示重新查询。即使提交相同数量，旧版本也会被拒绝。
- 重新 GET，使用最新版本再修改；数量与当前值相同时返回 200，版本不递增。
- 数量填 0 或负数返回 **400**；不存在的报价或报价行返回 **404**。这些错误不会部分修改报价。

## 对照 pgAdmin

打开 `vente → Schemas → sales → Tables`，或在 Query Tool 执行：

```sql
SELECT * FROM sales."Quotes"
WHERE "Id" = '22222222-2222-2222-2222-222222222222';

SELECT * FROM sales."QuoteLines"
WHERE "QuoteId" = '22222222-2222-2222-2222-222222222222'
ORDER BY "Position";
```

每次修改后重新执行查询，观察数量、金额与版本。手动示例保留在数据库中；自动化测试仅删除自己创建的随机报价，不清理这个示例。

## 代码对应关系

```text
Swagger → Program.cs 的 GET/PATCH
        → MediatR → GetQuoteHandler / ChangeQuoteLineQuantityHandler
        → IQuoteRepository → EfQuoteRepository → PostgreSQL
```

PATCH 在处理器内调用 `Quote.ChangeLineQuantity` 完成计算，再保存。HTTP 请求体 `ChangeQuantityRequest` 只有数量和原版本，报价和行 ID 来自 URL；不接受客户端传入计算金额。

## 从零创建一份报价

1. 在 **POST /quotes** 执行预填的项目 ID `55555555-5555-5555-5555-555555555555`。成功后复制返回的 `quoteId`，初始 `version` 为 1。
2. 在 **POST /quotes/{quoteId}/lines** 把路径的默认报价 ID 换成刚返回的 ID，将默认请求体中的 `expectedVersion` 改为 1，其余示例值可直接保留。数量 10、售价 20，行金额和总额均为 200，版本变成 2。复制返回的 `lineId`。
3. 在 **PATCH .../sales-unit-price** 填入该报价和行 ID，保留示例售价 22，将 `expectedVersion` 改为 2。总额变成 220，版本为 3。
4. 在 **PATCH .../quantity** 使用该报价和行 ID，数量 120、`expectedVersion` 为 3。总额变成 2640，版本为 4。
5. 在 **DELETE .../lines/{lineId}** 使用同一报价和行 ID，查询参数 `expectedVersion` 填 4。总额变成 0，版本为 5；再次 GET 可以看到空的 `lines`。之后仍可继续添加新行。

新报价和行的 ID 由服务器生成，不会自动替换 Swagger 其他表单中的预填 ID，所以从零操作时需要复制到对应位置。POST /quotes 同一个项目只能成功一次，重复提交返回 409；需要另一份报价时换一个新项目 ID。在 PowerShell 中执行 `[guid]::NewGuid()` 可生成一个新 ID。

创建报价目前只要求项目 ID 非空，并保证每个项目最多一份报价；尚未接入项目目录验证其存在性，也不创建项目或预算。预算/成本联动属于后续服务。

## 添加行、修改售价和删除的输入边界

- 添加行需要工程项代码、说明、单位、正数量、非负销售单价与最新版本。`salesUnitPrice` 必须显式提供，省略不会自动变成免费行。
- 单价可填 0，但不能为负数；1.005 这样的精细单价会保留，乘以数量后才舍入行金额。相同单价提交不改变版本。
- 删除必须在查询参数提供版本；不存在的行返回 404，旧版本返回 409。删除最后一行后保留空报价。
- 工程项代码与单位当前只校验非空，尚未接入 Library/Compositions 检查代码和单位是否匹配。
