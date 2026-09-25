using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineSalesUnitPrice;

public sealed record ChangeQuoteLineSalesUnitPriceCommand(Guid QuoteId, Guid LineId, decimal SalesUnitPrice,
    long ExpectedVersion) : IRequest<ChangeQuoteLineSalesUnitPriceResult>;
