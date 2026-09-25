using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.RemoveQuoteLine;

public sealed record RemoveQuoteLineCommand(Guid QuoteId, Guid LineId, long ExpectedVersion) : IRequest<RemoveQuoteLineResult>;
