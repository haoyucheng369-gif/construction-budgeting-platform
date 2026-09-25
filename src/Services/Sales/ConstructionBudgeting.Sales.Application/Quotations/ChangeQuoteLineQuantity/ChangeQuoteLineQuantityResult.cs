namespace ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;

public sealed record ChangeQuoteLineQuantityResult(
    Guid QuoteId,
    Guid LineId,
    decimal Quantity,
    decimal LineAmount,
    decimal TotalSalesAmount,
    string Currency,
    long Version);
