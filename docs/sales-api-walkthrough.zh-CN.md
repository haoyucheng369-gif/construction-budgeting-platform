# 报价查询与工程量修改

Sales 提供两个接口：查询完整报价、修改一行工程量。Swagger UI 是调用这两个接口的网页，返回值来自 PostgreSQL。

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

当前尚无创建报价、添加/删除行、修改售价的 HTTP 接口，示例初始化也不等于创建报价业务用例。这些按后续小步骤实现。
