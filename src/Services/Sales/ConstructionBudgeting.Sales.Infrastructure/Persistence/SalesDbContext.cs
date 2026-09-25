using ConstructionBudgeting.Sales.Domain.Quotations;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Quote> Quotes => Set<Quote>();

    // 关系表没有默认行顺序；读取时始终加载完整聚合，并显式按保存的顺序排列报价行。
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
        // 此处只处理报价的首次插入；保存已修改的聚合时，由 EfQuoteRepository 设置行顺序。
        foreach (var entry in ChangeTracker.Entries<Quote>().Where(entry => entry.State == EntityState.Added))
        {
            for (var position = 0; position < entry.Entity.Lines.Count; position++)
            {
                Entry(entry.Entity.Lines[position]).Property<int>("Position").CurrentValue = position;
            }
        }
    }
}
