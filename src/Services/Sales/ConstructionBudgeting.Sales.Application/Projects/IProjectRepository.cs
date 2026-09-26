using ConstructionBudgeting.Sales.Domain.Projects;

namespace ConstructionBudgeting.Sales.Application.Projects;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid projectId, CancellationToken cancellationToken);
}
