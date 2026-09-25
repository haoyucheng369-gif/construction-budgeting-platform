namespace ConstructionBudgeting.Sales.Application.Quotations.AddQuoteLine;

public sealed record AddQuoteLineResult(Guid QuoteId, Guid LineId, decimal Quantity,
    decimal SalesUnitPrice, decimal LineAmount, decimal TotalSalesAmount, string Currency, long Version);
