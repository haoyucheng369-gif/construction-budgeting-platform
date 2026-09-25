using ConstructionBudgeting.Sales.Domain.Quotations;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Quote> Quotes => Set<Quote>();

    // Relational tables have no implicit row order. Always load the complete ordered aggregate.
    public IQueryable<Quote> ReadQuotes() => Quotes.AsNoTracking()
        .Include(quote => quote.Lines.OrderBy(line => EF.Property<int>(line, "Position")));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("sales");
        modelBuilder.ApplyConfiguration(new QuoteConfiguration());
        modelBuilder.ApplyConfiguration(new QuoteLineConfiguration());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetInitialLinePositions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetInitialLinePositions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetInitialLinePositions()
    {
        // Initial inserts only. Replacing/editing tracked immutable lines is handled in the future repository.
        foreach (var entry in ChangeTracker.Entries<Quote>().Where(entry => entry.State == EntityState.Added))
        {
            for (var position = 0; position < entry.Entity.Lines.Count; position++)
            {
                Entry(entry.Entity.Lines[position]).Property<int>("Position").CurrentValue = position;
            }
        }
    }
}
