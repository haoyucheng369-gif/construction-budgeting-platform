using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class QuoteRemovalTests
{
    [Fact]
    public void Removing_a_line_updates_total_and_preserves_remaining_lines_and_order()
    {
        var quote = CreateQuote();
        var first = AddPainting(quote);
        var removed = AddPainting(quote, price: 30m);
        var last = AddPainting(quote, price: 40m);

        quote.RemoveLine(removed.Id);

        Assert.Equal(new Money(6000m), quote.TotalSalesAmount);
        Assert.Collection(quote.Lines,
            line => Assert.Same(first, line),
            line => Assert.Same(last, line));
    }

    [Fact]
    public void Removing_the_last_line_keeps_an_empty_quote_with_its_identity()
    {
        var quote = CreateQuote();
        var quoteId = quote.Id;
        var projectId = quote.ProjectId;
        var line = AddPainting(quote);

        quote.RemoveLine(line.Id);

        Assert.Empty(quote.Lines);
        Assert.Equal(new Money(0m), quote.TotalSalesAmount);
        Assert.Equal(quoteId, quote.Id);
        Assert.Equal(projectId, quote.ProjectId);

        var added = AddPainting(quote);
        Assert.Same(added, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Removing_a_free_line_changes_the_collection_without_changing_the_amount()
    {
        var quote = CreateQuote();
        var paid = AddPainting(quote);
        var free = AddPainting(quote, price: 0m);

        quote.RemoveLine(free.Id);

        Assert.Same(paid, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Invalid_line_id_leaves_the_quote_unchanged(bool emptyId)
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);
        var total = quote.TotalSalesAmount;

        if (emptyId)
        {
            Assert.Throws<ArgumentException>(() => quote.RemoveLine(Guid.Empty));
        }
        else
        {
            Assert.Throws<KeyNotFoundException>(() => quote.RemoveLine(Guid.NewGuid()));
        }

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Same(total, quote.TotalSalesAmount);
    }

    [Fact]
    public void Repeated_removal_is_rejected_without_subtracting_the_amount_again()
    {
        var quote = CreateQuote();
        var removed = AddPainting(quote);
        var remaining = AddPainting(quote, price: 30m);
        quote.RemoveLine(removed.Id);
        var total = quote.TotalSalesAmount;

        Assert.Throws<KeyNotFoundException>(() => quote.RemoveLine(removed.Id));

        Assert.Same(remaining, Assert.Single(quote.Lines));
        Assert.Same(total, quote.TotalSalesAmount);
        Assert.Equal(new Money(3000m), total);
    }

    [Fact]
    public void Removal_uses_the_latest_quantity_and_price()
    {
        var quote = CreateQuote();
        var removed = AddPainting(quote);
        var remaining = AddPainting(quote, price: 30m);
        quote.ChangeLineQuantity(removed.Id, new Quantity(120m));
        quote.ChangeLineSalesUnitPrice(removed.Id, new Money(22m));
        Assert.Equal(new Money(5640m), quote.TotalSalesAmount);

        quote.RemoveLine(removed.Id);

        Assert.Same(remaining, Assert.Single(quote.Lines));
        Assert.Equal(new Money(3000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Removal_subtracts_the_rounded_line_amount()
    {
        var quote = CreateQuote();
        var removed = AddPainting(quote, quantity: 1m, price: 0.005m);
        var remaining = AddPainting(quote, quantity: 1m, price: 0.005m);

        quote.RemoveLine(removed.Id);

        Assert.Same(remaining, Assert.Single(quote.Lines));
        Assert.Equal(new Money(0.01m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Removing_a_line_does_not_affect_another_quote_with_the_same_line_id()
    {
        var first = CreateQuote();
        var second = CreateQuote();
        var removed = AddPainting(first);
        var other = second.AddLine(
            removed.Id, "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));

        first.RemoveLine(removed.Id);

        Assert.Empty(first.Lines);
        Assert.Equal(new Money(0m), first.TotalSalesAmount);
        Assert.Same(other, Assert.Single(second.Lines));
        Assert.Equal(new Money(2000m), second.TotalSalesAmount);
    }

    [Fact]
    public void Removed_lines_cannot_be_edited()
    {
        var quote = CreateQuote();
        var removed = AddPainting(quote);
        quote.RemoveLine(removed.Id);

        Assert.Throws<KeyNotFoundException>(
            () => quote.ChangeLineQuantity(removed.Id, new Quantity(120m)));
        Assert.Throws<KeyNotFoundException>(
            () => quote.ChangeLineSalesUnitPrice(removed.Id, new Money(22m)));

        Assert.Empty(quote.Lines);
        Assert.Equal(new Money(0m), quote.TotalSalesAmount);
    }

    private static Quote CreateQuote() => new(Guid.NewGuid(), Guid.NewGuid());

    private static QuoteLine AddPainting(Quote quote, decimal quantity = 100m, decimal price = 20m)
    {
        return quote.AddLine(
            Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(quantity), new Money(price));
    }
}
