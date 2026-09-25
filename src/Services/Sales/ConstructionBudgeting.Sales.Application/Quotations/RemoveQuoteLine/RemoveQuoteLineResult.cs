namespace ConstructionBudgeting.Sales.Application.Quotations.RemoveQuoteLine;

public sealed record RemoveQuoteLineResult(Guid QuoteId, Guid LineId, decimal TotalSalesAmount, string Currency, long Version);
