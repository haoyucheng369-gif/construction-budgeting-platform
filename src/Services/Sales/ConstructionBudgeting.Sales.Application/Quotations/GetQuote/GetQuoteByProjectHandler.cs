using ConstructionBudgeting.Sales.Application.Projects;
using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.GetQuote;

public sealed class GetQuoteByProjectHandler(IQuoteRepository repository, IProjectRepository projects)
    : IRequestHandler<GetQuoteByProjectQuery, GetQuoteResult?>
{
    public async Task<GetQuoteResult?> Handle(GetQuoteByProjectQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.ProjectId == Guid.Empty)
        {
            throw new ArgumentException("A project must have an ID.", nameof(request.ProjectId));
        }

        var quote = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (quote is not null) return GetQuoteResult.FromQuote(quote);

        if (!await projects.ExistsAsync(request.ProjectId, cancellationToken))
            throw new ProjectNotFoundException(request.ProjectId);

        return null;
    }
}
