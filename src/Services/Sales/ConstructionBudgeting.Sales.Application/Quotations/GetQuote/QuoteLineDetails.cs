namespace ConstructionBudgeting.Sales.Application.Quotations.GetQuote;

public sealed record QuoteLineDetails(
    Guid LineId,
    string WorkItemCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal SalesUnitPrice,
    decimal LineAmount);
