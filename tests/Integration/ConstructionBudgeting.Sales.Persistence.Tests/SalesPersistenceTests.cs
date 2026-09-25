using ConstructionBudgeting.Sales.Domain.Quotations;
using ConstructionBudgeting.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ConstructionBudgeting.Sales.Persistence.Tests;

public sealed partial class SalesPersistenceTests
{
    [Fact]
    public void Migration_snapshot_matches_the_current_model()
    {
        using var context = new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused",
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "sales")).Options);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [PostgresFact]
    public async Task New_context_restores_identity_order_values_amounts_and_edited_version()
    {
        await MigrateAsync();
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid());
        // 故意按与 ID 排序相反的顺序插入，防止主键顺序碰巧正确而掩盖行顺序映射错误。
        var painting = quote.AddLine(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));
        var floor = quote.AddLine(Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "FLOOR", "Floor installation", "m2", new Quantity(2.5m), new Money(19.99m));
        quote.ChangeLineQuantity(painting.Id, new Quantity(120m));
        quote.ChangeLineSalesUnitPrice(painting.Id, new Money(22m));
        var expectedVersion = quote.Version;
        try
        {
            await using (var write = CreateContext())
            {
                write.Quotes.Add(quote);
                await write.SaveChangesAsync();
            }

            await using var read = CreateContext();
            var restored = await read.ReadQuotes().SingleAsync(item => item.Id == quote.Id);

            Assert.NotSame(quote, restored);
            Assert.Equal(quote.ProjectId, restored.ProjectId);
            Assert.Equal(expectedVersion, restored.Version);
            Assert.Equal(new Money(2689.98m), restored.TotalSalesAmount);
            Assert.Collection(restored.Lines,
                line =>
                {
                    Assert.Equal(painting.Id, line.Id);
                    Assert.Equal("PAINT", line.WorkItemCode);
                    Assert.Equal("Wall painting", line.Description);
                    Assert.Equal("m2", line.Unit);
                    Assert.Equal(new Quantity(120m), line.Quantity);
                    Assert.Equal(new Money(22m), line.SalesUnitPrice);
                    Assert.Equal(new Money(2640m), line.LineAmount);
                },
                line =>
                {
                    Assert.Equal(floor.Id, line.Id);
                    Assert.Equal(floor.Quantity, line.Quantity);
                    Assert.Equal(floor.SalesUnitPrice, line.SalesUnitPrice);
                    Assert.Equal(new Money(49.98m), line.LineAmount);
                });
            Assert.Empty(read.ChangeTracker.Entries());
            var collection = Assert.IsAssignableFrom<IList<QuoteLine>>(restored.Lines);
            Assert.Throws<NotSupportedException>(() => collection.Clear());
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    [PostgresFact]
    public async Task Empty_quote_retains_version_after_deleting_its_last_line_before_first_save()
    {
        await MigrateAsync();
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid());
        var line = quote.AddLine(Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));
        quote.RemoveLine(line.Id);
        try
        {
            await using (var write = CreateContext())
            {
                write.Quotes.Add(quote);
                await write.SaveChangesAsync();
            }
            await using var read = CreateContext();
            var restored = await read.ReadQuotes().SingleAsync(item => item.Id == quote.Id);
            Assert.Empty(restored.Lines);
            Assert.Equal(3L, restored.Version);
            Assert.Equal(new Money(0m), restored.TotalSalesAmount);
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    [PostgresFact]
    public async Task Decimal_precision_and_quote_scoped_line_ids_survive_storage()
    {
        await MigrateAsync();
        var first = new Quote(Guid.NewGuid(), Guid.NewGuid());
        var second = new Quote(Guid.NewGuid(), Guid.NewGuid());
        var sharedLineId = Guid.NewGuid();
        const decimal tinyQuantity = 0.0000000000000000000000000001m;
        first.AddLine(sharedLineId, "PAINT", "Fine quantity", "m2", new Quantity(tinyQuantity), new Money(1.005m));
        first.AddLine(Guid.NewGuid(), "PAINT", "Fractional price", "m2", new Quantity(100m), new Money(1.005m));
        second.AddLine(sharedLineId, "PAINT", "Decimal boundary", "m2", new Quantity(1m), new Money(decimal.MaxValue));
        try
        {
            await using (var write = CreateContext())
            {
                write.Quotes.AddRange(first, second);
                await write.SaveChangesAsync();
            }
            await using var read = CreateContext();
            var restoredFirst = await read.ReadQuotes().SingleAsync(item => item.Id == first.Id);
            var restoredSecond = await read.ReadQuotes().SingleAsync(item => item.Id == second.Id);
            Assert.Equal(new Quantity(tinyQuantity), restoredFirst.Lines[0].Quantity);
            Assert.Equal(new Money(1.005m), restoredFirst.Lines[0].SalesUnitPrice);
            Assert.Equal(new Money(100.50m), restoredFirst.TotalSalesAmount);
            Assert.Equal(sharedLineId, Assert.Single(restoredSecond.Lines).Id);
            Assert.Equal(new Money(decimal.MaxValue), restoredSecond.TotalSalesAmount);
        }
        finally { await DeleteQuotesAsync(first.Id, second.Id); }
    }

    [PostgresFact]
    public async Task Database_rejects_a_second_quote_for_the_same_project()
    {
        await MigrateAsync();
        var first = new Quote(Guid.NewGuid(), Guid.NewGuid());
        var second = new Quote(Guid.NewGuid(), first.ProjectId);
        try
        {
            await using (var write = CreateContext())
            {
                write.Quotes.Add(first);
                await write.SaveChangesAsync();
            }
            await using (var duplicate = CreateContext())
            {
                duplicate.Quotes.Add(second);
                var exception = await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
                Assert.Equal(PostgresErrorCodes.UniqueViolation,
                    Assert.IsType<PostgresException>(exception.InnerException).SqlState);
            }
            await using var read = CreateContext();
            Assert.Equal(first.Id, (await read.ReadQuotes().SingleAsync(item => item.ProjectId == first.ProjectId)).Id);
        }
        finally { await DeleteQuotesAsync(first.Id, second.Id); }
    }

    [PostgresFact]
    public async Task Database_rejects_zero_quantity_without_changing_the_stored_line()
    {
        await MigrateAsync();
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid());
        var line = quote.AddLine(Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));
        try
        {
            await using (var write = CreateContext())
            {
                write.Quotes.Add(quote);
                await write.SaveChangesAsync();
                var exception = await Assert.ThrowsAsync<PostgresException>(() => write.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE sales.\"QuoteLines\" SET \"Quantity\" = 0 WHERE \"QuoteId\" = {quote.Id} AND \"Id\" = {line.Id}"));
                Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            }
            await using var read = CreateContext();
            var restored = await read.ReadQuotes().SingleAsync(item => item.Id == quote.Id);
            Assert.Equal(new Quantity(100m), Assert.Single(restored.Lines).Quantity);
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    private static SalesDbContext CreateContext() => new SalesDbContextFactory().CreateDbContext([]);

    private static async Task MigrateAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    private static async Task DeleteQuotesAsync(params Guid[] quoteIds)
    {
        await using var cleanup = CreateContext();
        // 只清理本测试随机创建的报价 ID，绝不删除整个数据库或 schema。
        await cleanup.Quotes.Where(quote => quoteIds.Contains(quote.Id)).ExecuteDeleteAsync();
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SALES_PERSISTENCE_TESTS") != "1")
        {
            Skip = "Requires Compose PostgreSQL; run scripts/Test-SalesPersistence.ps1.";
        }
    }
}
