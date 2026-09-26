using MediatR;

namespace ConstructionBudgeting.Sales.Application.Projects;

public sealed record ListProjectsQuery() : IRequest<IReadOnlyList<ProjectDetails>>;

public sealed class ListProjectsHandler(IProjectRepository repository)
    : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectDetails>>
{
    public async Task<IReadOnlyList<ProjectDetails>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var projects = await repository.ListAsync(cancellationToken);
        return projects.Select(project => new ProjectDetails(project.Id, project.Name)).ToArray();
    }
}
