using ConstructionBudgeting.Sales.Domain.Quotations;
using ConstructionBudgeting.Sales.Application.Projects;
using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;

public sealed class CreateQuoteHandler(IQuoteRepository repository, IProjectRepository projects)
    : IRequestHandler<CreateQuoteCommand, CreateQuoteResult>
{
    public async Task<CreateQuoteResult> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.ProjectId == Guid.Empty)
            throw new ArgumentException("A quote must belong to a project.", nameof(request.ProjectId));
        if (!await projects.ExistsAsync(request.ProjectId, cancellationToken))
            throw new ProjectNotFoundException(request.ProjectId);

        var quote = new Quote(Guid.NewGuid(), request.ProjectId);
        await repository.AddAsync(quote, cancellationToken);
        return new CreateQuoteResult(quote.Id, quote.ProjectId, quote.Version,
            quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency);
    }
}
