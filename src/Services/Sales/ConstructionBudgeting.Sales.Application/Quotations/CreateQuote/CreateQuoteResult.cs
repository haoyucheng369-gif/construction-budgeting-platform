namespace ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;

public sealed record CreateQuoteResult(Guid QuoteId, Guid ProjectId, long Version, decimal TotalSalesAmount, string Currency);
