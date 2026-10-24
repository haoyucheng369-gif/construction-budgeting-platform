using ConstructionBudgeting.Sales.Application.Quotations.AddQuoteLine;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineSalesUnitPrice;
using ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;
using ConstructionBudgeting.Sales.Application.Quotations.GetQuote;
using ConstructionBudgeting.Sales.Application.Quotations.RemoveQuoteLine;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ConstructionBudgeting.Sales.Api.Controllers;

[ApiController]
[Route("quotes")]
[Tags("报价")]
public sealed class QuotesController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "CreateQuote")]
    [EndpointSummary("创建空报价")]
    [EndpointDescription("先创建或选择项目，并填写其 projectId；项目不存在返回 404，同一项目已有报价返回 409。返回的 quoteId 可用于后续添加行。")]
    [ProducesResponseType(typeof(CreateQuoteResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateQuoteCommand(request.ProjectId), cancellationToken);
        return CreatedAtRoute("GetQuote", new { id = result.QuoteId }, result);
    }

    [HttpGet("{id:guid}", Name = "GetQuote")]
    [EndpointSummary("查询报价及全部报价行")]
    [EndpointDescription("先查询并记下当前 version；修改时须提交最新版本。")]
    [ProducesResponseType(typeof(GetQuoteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<GetQuoteResult>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetQuoteQuery(id), cancellationToken));

    [HttpGet("by-project/{projectId:guid}", Name = "GetQuoteByProject")]
    [EndpointSummary("按项目查询报价")]
    [EndpointDescription("已有报价返回 200；项目存在但尚无报价返回 204；项目不存在返回 404。")]
    [ProducesResponseType(typeof(GetQuoteResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<GetQuoteResult>> GetByProject(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetQuoteByProjectQuery(projectId), cancellationToken);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpPost("{quoteId:guid}/lines", Name = "AddQuoteLine")]
    [EndpointSummary("添加报价行")]
    [EndpointDescription("服务器生成 lineId。填写工程项、单位、数量、售价和最新 expectedVersion；金额由后端计算。")]
    [ProducesResponseType(typeof(AddQuoteLineResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AddLine(Guid quoteId, [FromBody] AddQuoteLineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddQuoteLineCommand(quoteId, request.WorkItemCode,
            request.Description, request.Unit, request.Quantity, request.SalesUnitPrice,
            request.ExpectedVersion), cancellationToken);
        return CreatedAtRoute("GetQuote", new { id = result.QuoteId }, result);
    }

    [HttpPatch("{quoteId:guid}/lines/{lineId:guid}/quantity", Name = "ChangeQuoteLineQuantity")]
    [EndpointSummary("修改一行工程量")]
    [EndpointDescription("quantity 填新数量，expectedVersion 填刚才查询得到的 version；版本过期返回 409。")]
    [ProducesResponseType(typeof(ChangeQuoteLineQuantityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangeQuantity(Guid quoteId, Guid lineId,
        [FromBody] ChangeQuantityRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ChangeQuoteLineQuantityCommand(
            quoteId, lineId, request.Quantity, request.ExpectedVersion), cancellationToken));

    [HttpPatch("{quoteId:guid}/lines/{lineId:guid}/sales-unit-price", Name = "ChangeQuoteLineSalesUnitPrice")]
    [EndpointSummary("修改一行销售单价")]
    [EndpointDescription("salesUnitPrice 可以为 0，不能为负数；须使用最新 expectedVersion。")]
    [ProducesResponseType(typeof(ChangeQuoteLineSalesUnitPriceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangeSalesUnitPrice(Guid quoteId, Guid lineId,
        [FromBody] ChangeSalesUnitPriceRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ChangeQuoteLineSalesUnitPriceCommand(
            quoteId, lineId, request.SalesUnitPrice, request.ExpectedVersion), cancellationToken));

    [HttpDelete("{quoteId:guid}/lines/{lineId:guid}", Name = "RemoveQuoteLine")]
    [EndpointSummary("删除一行报价")]
    [EndpointDescription("在查询参数中填写最新 expectedVersion；删除最后一行后保留空报价。")]
    [ProducesResponseType(typeof(RemoveQuoteLineResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RemoveLine(Guid quoteId, Guid lineId,
        [FromQuery, BindRequired] long expectedVersion, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RemoveQuoteLineCommand(
            quoteId, lineId, expectedVersion), cancellationToken));
}
