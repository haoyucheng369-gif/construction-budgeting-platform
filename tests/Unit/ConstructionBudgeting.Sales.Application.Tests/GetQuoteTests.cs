using ConstructionBudgeting.Sales.Application;
using ConstructionBudgeting.Sales.Application.Quotations;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;
using ConstructionBudgeting.Sales.Application.Quotations.GetQuote;
using ConstructionBudgeting.Sales.Domain.Quotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ConstructionBudgeting.Sales.Application.Tests;

public sealed class GetQuoteTests
{
    [Fact]
    public async Task Query_returns_quote_and_line_fields_in_order_without_modifying_or_saving()
    {
        var quote = CreateQuote();
        var first = AddPainting(quote);
        var second = quote.AddLine(
            Guid.NewGuid(), "FLOOR", "Floor installation", "m2", new Quantity(2.5m), new Money(19.99m));
        var version = quote.Version;
        var total = quote.TotalSalesAmount;
        var repository = new StubQuoteRepository(quote);
        using var cancellation = new CancellationTokenSource();

        var result = await new GetQuoteHandler(repository).Handle(new(quote.Id), cancellation.Token);

        Assert.Equal(quote.Id, result.QuoteId);
        Assert.Equal(quote.ProjectId, result.ProjectId);
        Assert.Equal(version, result.Version);
        Assert.Equal(2049.98m, result.TotalSalesAmount);
        Assert.Equal("EUR", result.Currency);
        Assert.Collection(result.Lines,
            line => Assert.Equal(new QuoteLineDetails(first.Id, "PAINT", "Wall painting", "m2", 100m, 20m, 2000m), line),
            line => Assert.Equal(new QuoteLineDetails(second.Id, "FLOOR", "Floor installation", "m2", 2.5m, 19.99m, 49.98m), line));
        Assert.Collection(quote.Lines,
            line => Assert.Same(first, line),
            line => Assert.Same(second, line));
        Assert.Equal(version, quote.Version);
        Assert.Same(total, quote.TotalSalesAmount);
        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal(quote.Id, repository.RequestedQuoteId);
        Assert.Equal(cancellation.Token, repository.ReadToken);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Empty_quote_returns_an_empty_collection_and_zero_amount()
    {
        var quote = CreateQuote();
        var repository = new StubQuoteRepository(quote);

        var result = await new GetQuoteHandler(repository).Handle(new(quote.Id), CancellationToken.None);

        Assert.Empty(result.Lines);
        Assert.Equal(0m, result.TotalSalesAmount);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(1L, result.Version);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Returned_snapshot_is_read_only_and_does_not_follow_later_quote_changes()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote);
        var repository = new StubQuoteRepository(quote);
        var handler = new GetQuoteHandler(repository);
        var before = await handler.Handle(new(quote.Id), CancellationToken.None);

        var collection = Assert.IsAssignableFrom<IList<QuoteLineDetails>>(before.Lines);
        Assert.Throws<NotSupportedException>(() => collection.Clear());

        quote.ChangeLineQuantity(line.Id, new Quantity(120m));
        quote.RemoveLine(line.Id);
        var after = await handler.Handle(new(quote.Id), CancellationToken.None);

        Assert.Equal(100m, Assert.Single(before.Lines).Quantity);
        Assert.Equal(2000m, before.TotalSalesAmount);
        Assert.Equal(2L, before.Version);
        Assert.Empty(after.Lines);
        Assert.Equal(0m, after.TotalSalesAmount);
        Assert.Equal(4L, after.Version);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Missing_quote_throws_without_saving()
    {
        var repository = new StubQuoteRepository(null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new GetQuoteHandler(repository).Handle(new(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Empty_id_is_rejected_before_repository_access()
    {
        var repository = new StubQuoteRepository(null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            new GetQuoteHandler(repository).Handle(new(Guid.Empty), CancellationToken.None));

        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Null_query_is_rejected_before_repository_access()
    {
        var repository = new StubQuoteRepository(null);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            new GetQuoteHandler(repository).Handle(null!, CancellationToken.None));

        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Already_cancelled_query_does_not_access_repository()
    {
        var repository = new StubQuoteRepository(null);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GetQuoteHandler(repository).Handle(new(Guid.NewGuid()), cancellation.Token));

        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Read_failure_is_propagated_instead_of_returning_an_empty_quote()
    {
        var failure = new IOException("Storage unavailable.");
        var repository = new StubQuoteRepository(null) { ReadFailure = failure };

        var actual = await Record.ExceptionAsync(() =>
            new GetQuoteHandler(repository).Handle(new(Guid.NewGuid()), CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Mediator_dispatches_query_and_command_with_separate_read_and_write_behavior()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote);
        var repository = new StubQuoteRepository(quote);
        var services = new ServiceCollection();
        services.AddSalesApplication();
        services.AddScoped<IQuoteRepository>(_ => repository);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var before = await sender.Send(new GetQuoteQuery(quote.Id));
        Assert.Equal(0, repository.SaveCalls);
        await sender.Send(new ChangeQuoteLineQuantityCommand(quote.Id, line.Id, 120m, before.Version));
        var after = await sender.Send(new GetQuoteQuery(quote.Id));

        Assert.Equal(2000m, before.TotalSalesAmount);
        Assert.Equal(2400m, after.TotalSalesAmount);
        Assert.Equal(3L, after.Version);
        Assert.Equal(3, repository.ReadCalls);
        Assert.Equal(1, repository.SaveCalls);
    }

    private static Quote CreateQuote() => new(Guid.NewGuid(), Guid.NewGuid());

    private static QuoteLine AddPainting(Quote quote) => quote.AddLine(
        Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));

    // 使用传入的聚合记录仓储调用，不模拟数据库持久化行为。
    private sealed class StubQuoteRepository(Quote? quote) : IQuoteRepository
    {
        public int ReadCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public Guid RequestedQuoteId { get; private set; }
        public CancellationToken ReadToken { get; private set; }
        public Exception? ReadFailure { get; init; }

        public Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
        {
            ReadCalls++;
            RequestedQuoteId = quoteId;
            ReadToken = cancellationToken;
            return ReadFailure is null
                ? Task.FromResult(quote?.Id == quoteId ? quote : null)
                : Task.FromException<Quote?>(ReadFailure);
        }

        public Task SaveAsync(Quote editedQuote, long expectedVersion, CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }
}
