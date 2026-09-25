using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.AddQuoteLine;

public sealed record AddQuoteLineCommand(Guid QuoteId, string WorkItemCode, string Description,
    string Unit, decimal Quantity, decimal SalesUnitPrice, long ExpectedVersion) : IRequest<AddQuoteLineResult>;
