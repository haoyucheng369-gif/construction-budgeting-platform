namespace ConstructionBudgeting.Sales.Application.Quotations.GetQuote;

public sealed record GetQuoteResult(
    Guid QuoteId,
    Guid ProjectId,
    long Version,
    decimal TotalSalesAmount,
    string Currency,
    IReadOnlyList<QuoteLineDetails> Lines);
