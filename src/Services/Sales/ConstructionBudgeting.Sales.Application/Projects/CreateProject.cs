using ConstructionBudgeting.Sales.Domain.Projects;
using MediatR;

namespace ConstructionBudgeting.Sales.Application.Projects;

public sealed record CreateProjectCommand(string Name) : IRequest<ProjectDetails>;

public sealed class CreateProjectHandler(IProjectRepository repository)
    : IRequestHandler<CreateProjectCommand, ProjectDetails>
{
    public async Task<ProjectDetails> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var project = new Project(Guid.NewGuid(), request.Name);
        await repository.AddAsync(project, cancellationToken);
        return new ProjectDetails(project.Id, project.Name);
    }
}
