using ConstructionBudgeting.Sales.Application;
using ConstructionBudgeting.Sales.Application.Quotations;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;
using ConstructionBudgeting.Sales.Domain.Quotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ConstructionBudgeting.Sales.Application.Tests;

public sealed class ChangeQuoteLineQuantityTests
{
    [Fact]
    public async Task Successful_change_saves_with_original_version_and_returns_domain_amounts()
    {
        var quote = CreateQuote();
        var line = Assert.Single(quote.Lines);
        quote.AddLine(Guid.NewGuid(), "FLOOR", "Floor installation", "m2", new Quantity(50m), new Money(40m));
        var originalVersion = quote.Version;
        var repository = new StubQuoteRepository(quote);
        using var cancellation = new CancellationTokenSource();

        var result = await new ChangeQuoteLineQuantityHandler(repository).Handle(
            new(quote.Id, line.Id, 120m, originalVersion), cancellation.Token);

        Assert.Equal(new ChangeQuoteLineQuantityResult(
            quote.Id, line.Id, 120m, 2400m, 4400m, "EUR", originalVersion + 1), result);
        Assert.Same(quote, repository.SavedQuote);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(originalVersion, repository.ExpectedVersion);
        Assert.Equal(quote.Id, repository.RequestedQuoteId);
        Assert.Equal(cancellation.Token, repository.ReadToken);
        Assert.Equal(cancellation.Token, repository.SaveToken);
    }

    [Fact]
    public async Task Changed_quantity_is_saved_even_when_the_amount_remains_zero()
    {
        var quote = CreateQuote(price: 0m);
        var repository = new StubQuoteRepository(quote);

        var result = await new ChangeQuoteLineQuantityHandler(repository).Handle(
            CommandFor(quote, quantity: 120m), CancellationToken.None);

        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(120m, result.Quantity);
        Assert.Equal(0m, result.TotalSalesAmount);
        Assert.Equal(3L, result.Version);
    }

    [Fact]
    public async Task Equal_quantity_returns_current_values_without_saving()
    {
        var quote = CreateQuote();
        var repository = new StubQuoteRepository(quote);

        var result = await new ChangeQuoteLineQuantityHandler(repository).Handle(
            CommandFor(quote, quantity: 100.00m), CancellationToken.None);

        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal(2L, result.Version);
        Assert.Equal(2000m, result.TotalSalesAmount);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(120)]
    public async Task Stale_input_is_rejected_before_mutation_even_for_equal_quantity(int quantity)
    {
        var quote = CreateQuote();
        var original = Assert.Single(quote.Lines);
        var repository = new StubQuoteRepository(quote);

        var exception = await Assert.ThrowsAsync<QuoteConcurrencyException>(() =>
            new ChangeQuoteLineQuantityHandler(repository).Handle(
                new(quote.Id, original.Id, quantity, 1L), CancellationToken.None));

        Assert.Equal(quote.Id, exception.QuoteId);
        Assert.Equal(1L, exception.ExpectedVersion);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(2L, quote.Version);
    }

    [Theory]
    [InlineData("quote-id")]
    [InlineData("line-id")]
    [InlineData("zero-quantity")]
    [InlineData("negative-quantity")]
    [InlineData("zero-version")]
    [InlineData("negative-version")]
    public async Task Invalid_input_is_rejected_before_repository_access(string invalidField)
    {
        var quote = CreateQuote();
        var repository = new StubQuoteRepository(quote);
        var command = CommandFor(quote);
        command = invalidField switch
        {
            "quote-id" => command with { QuoteId = Guid.Empty },
            "line-id" => command with { LineId = Guid.Empty },
            "zero-quantity" => command with { Quantity = 0m },
            "negative-quantity" => command with { Quantity = -1m },
            "zero-version" => command with { ExpectedVersion = 0L },
            "negative-version" => command with { ExpectedVersion = -1L },
            _ => throw new ArgumentException("Unknown invalid field.", nameof(invalidField))
        };

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            new ChangeQuoteLineQuantityHandler(repository).Handle(command, CancellationToken.None));

        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal(2L, quote.Version);
    }

    [Fact]
    public async Task Missing_quote_does_not_save()
    {
        var repository = new StubQuoteRepository(null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new ChangeQuoteLineQuantityHandler(repository).Handle(
                new(Guid.NewGuid(), Guid.NewGuid(), 120m, 1L), CancellationToken.None));

        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Missing_line_does_not_save_or_change_the_quote()
    {
        var quote = CreateQuote();
        var original = Assert.Single(quote.Lines);
        var repository = new StubQuoteRepository(quote);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new ChangeQuoteLineQuantityHandler(repository).Handle(
                CommandFor(quote) with { LineId = Guid.NewGuid() }, CancellationToken.None));

        Assert.Equal(0, repository.SaveCalls);
        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(2L, quote.Version);
    }

    [Fact]
    public async Task Domain_calculation_failure_does_not_save()
    {
        var quote = CreateQuote(quantity: 1m, price: decimal.MaxValue);
        var original = Assert.Single(quote.Lines);
        var repository = new StubQuoteRepository(quote);

        await Assert.ThrowsAsync<OverflowException>(() =>
            new ChangeQuoteLineQuantityHandler(repository).Handle(
                CommandFor(quote, quantity: 2m), CancellationToken.None));

        Assert.Equal(0, repository.SaveCalls);
        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(2L, quote.Version);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Save_conflicts_and_storage_failures_propagate_without_retry(bool conflict)
    {
        var quote = CreateQuote();
        Exception failure = conflict
            ? new QuoteConcurrencyException(quote.Id, quote.Version)
            : new IOException("Storage unavailable.");
        var repository = new StubQuoteRepository(quote) { SaveFailure = failure };

        var actual = await Record.ExceptionAsync(() =>
            new ChangeQuoteLineQuantityHandler(repository).Handle(CommandFor(quote), CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(2L, repository.ExpectedVersion);
        // The loaded unit of work is already edited and must be discarded after a failed save.
        Assert.Equal(3L, quote.Version);
    }

    [Fact]
    public async Task Already_cancelled_request_does_not_access_repository()
    {
        var quote = CreateQuote();
        var repository = new StubQuoteRepository(quote);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ChangeQuoteLineQuantityHandler(repository).Handle(CommandFor(quote), cancellation.Token));

        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal(2L, quote.Version);
    }

    [Fact]
    public async Task Registered_mediator_dispatches_command_to_handler()
    {
        var quote = CreateQuote();
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

        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(CommandFor(quote));

        Assert.Equal(2400m, result.TotalSalesAmount);
        Assert.Equal(3L, result.Version);
        Assert.Equal(1, repository.SaveCalls);
    }

    private static Quote CreateQuote(decimal quantity = 100m, decimal price = 20m)
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid());
        quote.AddLine(Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(quantity), new Money(price));
        return quote;
    }

    private static ChangeQuoteLineQuantityCommand CommandFor(Quote quote, decimal quantity = 120m) =>
        new(quote.Id, Assert.Single(quote.Lines).Id, quantity, quote.Version);

    // Records application interactions; this is not a database or a concurrency implementation.
    private sealed class StubQuoteRepository(Quote? quote) : IQuoteRepository
    {
        public int ReadCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public Guid RequestedQuoteId { get; private set; }
        public Quote? SavedQuote { get; private set; }
        public long ExpectedVersion { get; private set; }
        public CancellationToken ReadToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Exception? SaveFailure { get; init; }

        public Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
        {
            ReadCalls++;
            RequestedQuoteId = quoteId;
            ReadToken = cancellationToken;
            return Task.FromResult(quote?.Id == quoteId ? quote : null);
        }

        public Task SaveAsync(Quote editedQuote, long expectedVersion, CancellationToken cancellationToken)
        {
            SaveCalls++;
            SavedQuote = editedQuote;
            ExpectedVersion = expectedVersion;
            SaveToken = cancellationToken;
            return SaveFailure is null ? Task.CompletedTask : Task.FromException(SaveFailure);
        }
    }
}
