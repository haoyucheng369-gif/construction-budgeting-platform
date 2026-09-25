namespace ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineSalesUnitPrice;

public sealed record ChangeQuoteLineSalesUnitPriceResult(Guid QuoteId, Guid LineId, decimal SalesUnitPrice,
    decimal LineAmount, decimal TotalSalesAmount, string Currency, long Version);
