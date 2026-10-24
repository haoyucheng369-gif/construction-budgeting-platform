using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.GetQuote;

public sealed record GetQuoteByProjectQuery(Guid ProjectId) : IRequest<GetQuoteResult?>;
