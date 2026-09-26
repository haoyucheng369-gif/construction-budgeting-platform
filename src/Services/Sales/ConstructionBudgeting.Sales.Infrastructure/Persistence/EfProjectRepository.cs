using ConstructionBudgeting.Sales.Application.Projects;
using ConstructionBudgeting.Sales.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

public sealed class EfProjectRepository(IDbContextFactory<SalesDbContext> contextFactory) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Projects.Add(project);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Projects.AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Projects.AsNoTracking().OrderBy(project => project.Name)
            .ThenBy(project => project.Id).ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Projects.AnyAsync(project => project.Id == projectId, cancellationToken);
    }
}
