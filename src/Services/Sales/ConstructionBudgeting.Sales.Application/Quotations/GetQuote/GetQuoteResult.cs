using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Application.Quotations.GetQuote;

public sealed record GetQuoteResult(
    Guid QuoteId,
    Guid ProjectId,
    long Version,
    decimal TotalSalesAmount,
    string Currency,
    IReadOnlyList<QuoteLineDetails> Lines)
{
    public static GetQuoteResult FromQuote(Quote quote)
    {
        // 复制领域中的已计算金额，返回不跟随聚合变化的只读快照。
        var lines = quote.Lines.Select(line => new QuoteLineDetails(
            line.Id, line.WorkItemCode, line.Description, line.Unit,
            line.Quantity.Value, line.SalesUnitPrice.Amount, line.LineAmount.Amount)).ToArray();

        return new GetQuoteResult(
            quote.Id, quote.ProjectId, quote.Version,
            quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency,
            Array.AsReadOnly(lines));
    }
}
