using ConstructionBudgeting.Sales.Domain.Quotations;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

public static class SalesSampleData
{
    public static readonly Guid QuoteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task InitializeAsync(IDbContextFactory<SalesDbContext> factory,
        CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);

        // 重复执行不覆盖已有报价，也不重置用户修改。
        var existing = await context.Quotes.AsNoTracking()
            .SingleOrDefaultAsync(quote => quote.ProjectId == ProjectId || quote.Id == QuoteId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Id != QuoteId || existing.ProjectId != ProjectId)
                throw new InvalidOperationException("Sample identity is already used by another quotation.");
            return;
        }

        var quote = new Quote(QuoteId, ProjectId);
        quote.AddLine(Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "PAINT", "墙面涂装", "m2", new Quantity(100m), new Money(20m));
        quote.AddLine(Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "FLOOR", "地板铺设", "m2", new Quantity(2.5m), new Money(19.99m));
        context.Quotes.Add(quote);
        await context.SaveChangesAsync(cancellationToken);
    }
}
