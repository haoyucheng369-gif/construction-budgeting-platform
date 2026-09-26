using MediatR;

namespace ConstructionBudgeting.Sales.Application.Projects;

public sealed record GetProjectQuery(Guid ProjectId) : IRequest<ProjectDetails>;

public sealed class GetProjectHandler(IProjectRepository repository)
    : IRequestHandler<GetProjectQuery, ProjectDetails>
{
    public async Task<ProjectDetails> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProjectId == Guid.Empty)
            throw new ArgumentException("A project must have an ID.", nameof(request.ProjectId));

        var project = await repository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new ProjectNotFoundException(request.ProjectId);
        return new ProjectDetails(project.Id, project.Name);
    }
}
