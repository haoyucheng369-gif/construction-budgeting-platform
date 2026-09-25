using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Sales");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException("Set ConnectionStrings__Sales, or dot-source scripts/Use-SalesDatabase.ps1 first.");
        }

        return new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "sales"))
            .Options);
    }
}
