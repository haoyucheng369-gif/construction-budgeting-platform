using ConstructionBudgeting.Sales.Application.Quotations.AddQuoteLine;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineSalesUnitPrice;
using ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;
using ConstructionBudgeting.Sales.Application.Quotations.GetQuote;
using ConstructionBudgeting.Sales.Application.Quotations.RemoveQuoteLine;
using MediatR;

namespace ConstructionBudgeting.Sales.Api;

public static class QuoteEndpoints
{
    public static void MapQuoteEndpoints(this WebApplication app)
    {
        var quotes = app.MapGroup("/quotes").WithTags("报价");

        // HTTP 入口只转换参数，业务规则继续由应用处理器和 Quote 执行。
        quotes.MapPost("", async (CreateQuoteRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateQuoteCommand(request.ProjectId), cancellationToken);
            return TypedResults.CreatedAtRoute(value: result, routeName: "GetQuote", routeValues: new { id = result.QuoteId });
        })
            .WithName("CreateQuote")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("创建空报价")
            .WithDescription("每个项目只允许一份报价，重复创建返回 409。当前校验项目 ID 非空，尚未接入项目目录验证其存在性。返回 quoteId 可用于后续添加行。");

        quotes.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new GetQuoteQuery(id), cancellationToken)))
            .WithName("GetQuote")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("查询报价及全部报价行")
            .WithDescription("先执行查询，记下当前 version。所有修改使用最新版本；示例参数指向预置报价。");

        quotes.MapPost("/{quoteId:guid}/lines",
            async (Guid quoteId, AddQuoteLineRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new AddQuoteLineCommand(quoteId, request.WorkItemCode,
                    request.Description, request.Unit, request.Quantity, request.SalesUnitPrice, request.ExpectedVersion), cancellationToken);
                return TypedResults.CreatedAtRoute(value: result, routeName: "GetQuote", routeValues: new { id = result.QuoteId });
            })
            .WithName("AddQuoteLine")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("添加报价行")
            .WithDescription("服务器生成 lineId。填写工程项、单位、数量、售价及最新 expectedVersion；金额由后端计算。");

        quotes.MapPatch("/{quoteId:guid}/lines/{lineId:guid}/quantity",
            async (Guid quoteId, Guid lineId, ChangeQuantityRequest request, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new ChangeQuoteLineQuantityCommand(
                    quoteId, lineId, request.Quantity, request.ExpectedVersion), cancellationToken)))
            .WithName("ChangeQuoteLineQuantity")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("修改一行工程量")
            .WithDescription("quantity 填新数量；expectedVersion 填刚才查询得到的 version。版本过期返回 409，请重新查询。");

        quotes.MapPatch("/{quoteId:guid}/lines/{lineId:guid}/sales-unit-price",
            async (Guid quoteId, Guid lineId, ChangeSalesUnitPriceRequest request, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new ChangeQuoteLineSalesUnitPriceCommand(
                    quoteId, lineId, request.SalesUnitPrice, request.ExpectedVersion), cancellationToken)))
            .WithName("ChangeQuoteLineSalesUnitPrice")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("修改一行销售单价")
            .WithDescription("salesUnitPrice 可以为 0，不能为负数。使用最新 expectedVersion；同价提交不递增版本。");

        quotes.MapDelete("/{quoteId:guid}/lines/{lineId:guid}",
            async (Guid quoteId, Guid lineId, long expectedVersion, ISender sender, CancellationToken cancellationToken) =>
                TypedResults.Ok(await sender.Send(new RemoveQuoteLineCommand(
                    quoteId, lineId, expectedVersion), cancellationToken)))
            .WithName("RemoveQuoteLine")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("删除一行报价")
            .WithDescription("expectedVersion 在查询参数中填写最新版本。默认行 ID 是示例中的地板行；删除最后一行后保留空报价。");
    }
}
