using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;

public sealed record ChangeQuoteLineQuantityCommand(
    Guid QuoteId,
    Guid LineId,
    decimal Quantity,
    long ExpectedVersion) : IRequest<ChangeQuoteLineQuantityResult>;
