using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.GetQuote;

public sealed record GetQuoteQuery(Guid QuoteId) : IRequest<GetQuoteResult>;
