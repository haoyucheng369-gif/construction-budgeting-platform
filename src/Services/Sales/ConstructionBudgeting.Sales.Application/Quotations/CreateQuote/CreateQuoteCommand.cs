using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;

public sealed record CreateQuoteCommand(Guid ProjectId) : IRequest<CreateQuoteResult>;
