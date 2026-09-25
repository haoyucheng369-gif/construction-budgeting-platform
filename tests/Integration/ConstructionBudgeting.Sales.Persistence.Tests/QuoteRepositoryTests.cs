using ConstructionBudgeting.Sales.Application.Quotations;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;
using ConstructionBudgeting.Sales.Application.Quotations.GetQuote;
using ConstructionBudgeting.Sales.Domain.Quotations;
using ConstructionBudgeting.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ConstructionBudgeting.Sales.Persistence.Tests;

public sealed partial class SalesPersistenceTests
{
    [PostgresFact]
    public async Task Real_repository_connects_quantity_command_and_query_with_independent_snapshots()
    {
        var quote = await SeedQuoteAsync();
        try
        {
            var repository = new EfQuoteRepository(new TestContextFactory());
            var query = new GetQuoteHandler(repository);
            var before = await query.Handle(new GetQuoteQuery(quote.Id), default);
            var command = new ChangeQuoteLineQuantityHandler(repository);
            var result = await command.Handle(new ChangeQuoteLineQuantityCommand(
                quote.Id, quote.Lines[0].Id, 120m, before.Version), default);
            var after = await query.Handle(new GetQuoteQuery(quote.Id), default);

            Assert.Equal(2400m, result.LineAmount);
            Assert.Equal(2449.98m, after.TotalSalesAmount);
            Assert.Equal(before.Version + 1, after.Version);
            Assert.Equal(2049.98m, before.TotalSalesAmount);
            Assert.Equal(100m, before.Lines[0].Quantity);
            Assert.Equal(120m, after.Lines[0].Quantity);
            Assert.Equal(quote.ProjectId, after.ProjectId);
            Assert.Equal(quote.Lines.Select(line => line.Id), after.Lines.Select(line => line.LineId));

            var first = (await repository.GetByIdAsync(quote.Id, default))!;
            var second = (await repository.GetByIdAsync(quote.Id, default))!;
            Assert.NotSame(first, second);
            first.ChangeLineQuantity(first.Lines[0].Id, new Quantity(1m));
            Assert.Equal(120m, second.Lines[0].Quantity.Value);
            Assert.Null(await repository.GetByIdAsync(Guid.NewGuid(), default));
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    [PostgresFact]
    public async Task Repeated_saves_preserve_replacement_order_and_empty_drafts_without_touching_other_quotes()
    {
        var quote = await SeedQuoteAsync();
        var other = await SeedQuoteAsync(); // 不同报价可以使用相同的行 ID。
        try
        {
            var repository = new EfQuoteRepository(new TestContextFactory());
            var edited = (await repository.GetByIdAsync(quote.Id, default))!;
            var version = edited.Version;
            edited.RemoveLine(edited.Lines[0].Id);
            edited.ChangeLineSalesUnitPrice(edited.Lines[0].Id, new Money(21.005m));
            var extra = edited.AddLine(Guid.NewGuid(), "DOOR", "Door installation", "u",
                new Quantity(2m), new Money(100m));
            await repository.SaveAsync(edited, version, default);
            var restored = (await repository.GetByIdAsync(quote.Id, default))!;
            Assert.Equal(edited.Version, restored.Version);
            Assert.Equal(new Money(252.51m), restored.TotalSalesAmount);
            Assert.Equal(new[] { quote.Lines[1].Id, extra.Id }, restored.Lines.Select(line => line.Id));
            Assert.Equal(new Money(21.005m), restored.Lines[0].SalesUnitPrice);
            Assert.Equal("Floor installation", restored.Lines[0].Description);
            Assert.Equal("m2", restored.Lines[0].Unit);

            version = edited.Version;
            foreach (var line in edited.Lines.ToArray()) edited.RemoveLine(line.Id);
            await repository.SaveAsync(edited, version, default);
            restored = (await repository.GetByIdAsync(quote.Id, default))!;
            Assert.Empty(restored.Lines);
            Assert.Equal(new Money(0m), restored.TotalSalesAmount);
            Assert.Equal(edited.Version, restored.Version);

            var unaffected = (await repository.GetByIdAsync(other.Id, default))!;
            Assert.Equal(other.Version, unaffected.Version);
            Assert.Equal(other.TotalSalesAmount, unaffected.TotalSalesAmount);
            Assert.Equal(2, unaffected.Lines.Count);
        }
        finally { await DeleteQuotesAsync(quote.Id, other.Id); }
    }

    [PostgresFact]
    public async Task Two_writers_from_same_version_have_one_winner_and_no_partial_loser_changes()
    {
        var quote = await SeedQuoteAsync();
        try
        {
            // 每次仓储操作都会创建独立的数据库上下文，分别使用各自的连接。
            var repository = new EfQuoteRepository(new TestContextFactory());
            var first = (await repository.GetByIdAsync(quote.Id, default))!;
            var second = (await repository.GetByIdAsync(quote.Id, default))!;
            first.ChangeLineQuantity(first.Lines[0].Id, new Quantity(120m));
            second.ChangeLineSalesUnitPrice(second.Lines[1].Id, new Money(25m));

            var errors = await Task.WhenAll(
                Record.ExceptionAsync(() => repository.SaveAsync(first, quote.Version, default)),
                Record.ExceptionAsync(() => repository.SaveAsync(second, quote.Version, default)));
            Assert.Single(errors, error => error is null);
            var conflict = Assert.IsType<QuoteConcurrencyException>(Assert.Single(errors, error => error is not null));
            Assert.Equal(quote.Id, conflict.QuoteId);
            Assert.Equal(quote.Version, conflict.ExpectedVersion);
            var winner = errors[0] is null ? first : second;
            var restored = (await repository.GetByIdAsync(quote.Id, default))!;
            Assert.Equal(winner.Version, restored.Version);
            Assert.Equal(winner.TotalSalesAmount, restored.TotalSalesAmount);
            Assert.Equal(winner.Lines.Select(LineState), restored.Lines.Select(LineState));
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    [PostgresFact]
    public async Task Failure_after_header_update_and_line_deletion_rolls_back_the_entire_quote()
    {
        var quote = await SeedQuoteAsync();
        try
        {
            var failure = new FailLineSaveInterceptor();
            var repository = new EfQuoteRepository(new TestContextFactory(failure));
            var edited = (await repository.GetByIdAsync(quote.Id, default))!;
            edited.ChangeLineQuantity(edited.Lines[0].Id, new Quantity(120m));
            await Assert.ThrowsAsync<InjectedSaveException>(() => repository.SaveAsync(edited, quote.Version, default));
            Assert.True(failure.ReachedSave);

            var healthyRepository = new EfQuoteRepository(new TestContextFactory());
            var restored = (await healthyRepository.GetByIdAsync(quote.Id, default))!;
            Assert.Equal(quote.Version, restored.Version);
            Assert.Equal(quote.TotalSalesAmount, restored.TotalSalesAmount);
            Assert.Equal(quote.Lines.Select(LineState), restored.Lines.Select(LineState));
            // 失败的上下文和事务已释放，后续有效请求应能正常保存。
            restored.ChangeLineQuantity(restored.Lines[0].Id, new Quantity(110m));
            await healthyRepository.SaveAsync(restored, quote.Version, default);
            Assert.Equal(new Money(2249.98m),
                (await healthyRepository.GetByIdAsync(quote.Id, default))!.TotalSalesAmount);
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    [PostgresFact]
    public async Task Deleted_quote_is_a_conflict_and_is_not_recreated()
    {
        var quote = await SeedQuoteAsync();
        try
        {
            var repository = new EfQuoteRepository(new TestContextFactory());
            var edited = (await repository.GetByIdAsync(quote.Id, default))!;
            edited.ChangeLineQuantity(edited.Lines[0].Id, new Quantity(120m));
            await DeleteQuotesAsync(quote.Id);
            await Assert.ThrowsAsync<QuoteConcurrencyException>(() => repository.SaveAsync(edited, quote.Version, default));
            Assert.Null(await repository.GetByIdAsync(quote.Id, default));
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    [PostgresFact]
    public async Task Same_quantity_skips_storage_and_stale_commands_still_conflict()
    {
        var quote = await SeedQuoteAsync();
        try
        {
            var failure = new FailLineSaveInterceptor();
            var repository = new EfQuoteRepository(new TestContextFactory(failure));
            var handler = new ChangeQuoteLineQuantityHandler(repository);
            var command = new ChangeQuoteLineQuantityCommand(quote.Id, quote.Lines[0].Id, 100m, quote.Version);
            var result = await handler.Handle(command, default);
            Assert.Equal(quote.Version, result.Version);
            Assert.False(failure.ReachedSave);
            await Assert.ThrowsAsync<QuoteConcurrencyException>(() => handler.Handle(
                command with { ExpectedVersion = quote.Version - 1 }, default));
            Assert.Equal(quote.Version, (await repository.GetByIdAsync(quote.Id, default))!.Version);
        }
        finally { await DeleteQuotesAsync(quote.Id); }
    }

    private static object LineState(QuoteLine line) => new
    {
        line.Id, line.WorkItemCode, line.Description, line.Unit,
        line.Quantity, line.SalesUnitPrice, line.LineAmount
    };

    private static async Task<Quote> SeedQuoteAsync()
    {
        await MigrateAsync();
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid());
        quote.AddLine(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), "PAINT", "Wall painting", "m2",
            new Quantity(100m), new Money(20m));
        quote.AddLine(Guid.Parse("00000000-0000-0000-0000-000000000001"), "FLOOR", "Floor installation", "m2",
            new Quantity(2.5m), new Money(19.99m));
        await using var context = CreateContext();
        context.Quotes.Add(quote);
        await context.SaveChangesAsync();
        return quote;
    }

    private sealed class TestContextFactory(params IInterceptor[] interceptors) : IDbContextFactory<SalesDbContext>
    {
        public SalesDbContext CreateDbContext() => new(new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Sales"),
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "sales"))
            .AddInterceptors(interceptors).Options);
    }

    private sealed class InjectedSaveException : Exception;

    private sealed class FailLineSaveInterceptor : SaveChangesInterceptor
    {
        public bool ReachedSave { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = (SalesDbContext)eventData.Context!;
            var line = context.ChangeTracker.Entries<QuoteLine>().First();
            var quoteId = line.Property<Guid>("QuoteId").CurrentValue;
            // 确认旧行已被删除、表头已更新，且事务尚未结束，再注入故障以验证整体回滚。
            Assert.NotNull(context.Database.CurrentTransaction);
            Assert.Empty(await context.Set<QuoteLine>()
                .Where(item => EF.Property<Guid>(item, "QuoteId") == quoteId).AsNoTracking().ToListAsync(cancellationToken));
            Assert.Equal(4, await context.Quotes.Where(item => item.Id == quoteId)
                .Select(item => item.Version).SingleAsync(cancellationToken));
            ReachedSave = true;
            throw new InjectedSaveException();
        }
    }
}
