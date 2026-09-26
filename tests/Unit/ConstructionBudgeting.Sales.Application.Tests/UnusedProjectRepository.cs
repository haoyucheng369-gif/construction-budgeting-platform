using ConstructionBudgeting.Sales.Application.Projects;
using ConstructionBudgeting.Sales.Domain.Projects;

namespace ConstructionBudgeting.Sales.Application.Tests;

// 这两项测试只验证报价命令/查询的 MediatR 分发，项目仓储不会被调用。
internal sealed class UnusedProjectRepository : IProjectRepository
{
    public Task AddAsync(Project project, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<bool> ExistsAsync(Guid projectId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
