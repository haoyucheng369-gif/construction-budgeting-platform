using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class QuoteVersionTests
{
    [Fact]
    public void New_quote_starts_at_version_one()
    {
        Assert.Equal(1L, CreateQuote().Version);
    }

    [Fact]
    public void Each_successful_edit_advances_the_quote_version_once()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote);
        Assert.Equal(2L, quote.Version);

        quote.ChangeLineQuantity(line.Id, new Quantity(120m));
        Assert.Equal(3L, quote.Version);

        quote.ChangeLineSalesUnitPrice(line.Id, new Money(22m));
        Assert.Equal(4L, quote.Version);

        quote.RemoveLine(line.Id);
        Assert.Equal(5L, quote.Version);
        Assert.Empty(quote.Lines);

        AddPainting(quote);
        Assert.Equal(6L, quote.Version);
    }

    [Fact]
    public void Equal_quantity_and_price_do_not_advance_version()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote);
        var version = quote.Version;

        quote.ChangeLineQuantity(line.Id, new Quantity(100.00m));
        quote.ChangeLineSalesUnitPrice(line.Id, new Money(20.00m));

        Assert.Equal(version, quote.Version);
        Assert.Same(line, Assert.Single(quote.Lines));
    }

    [Fact]
    public void Free_line_addition_quantity_change_and_removal_still_advance_version()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote, price: 0m);
        Assert.Equal(2L, quote.Version);

        quote.ChangeLineQuantity(line.Id, new Quantity(120m));
        Assert.Equal(3L, quote.Version);

        quote.RemoveLine(line.Id);
        Assert.Equal(4L, quote.Version);
        Assert.Equal(new Money(0m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Price_change_advances_version_even_when_the_rounded_amount_matches()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote, quantity: 1m, price: 1.001m);
        var total = quote.TotalSalesAmount;

        quote.ChangeLineSalesUnitPrice(line.Id, new Money(1.002m));

        Assert.Equal(3L, quote.Version);
        Assert.Equal(total, quote.TotalSalesAmount);
        Assert.Equal(new Money(1.002m), Assert.Single(quote.Lines).SalesUnitPrice);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("invalid-addition")]
    [InlineData("null-quantity")]
    [InlineData("negative-price")]
    [InlineData("missing-quantity-line")]
    [InlineData("missing-price-line")]
    [InlineData("missing-removal-line")]
    [InlineData("empty-removal-id")]
    public void Rejected_edits_preserve_version_lines_and_total(string operation)
    {
        var quote = CreateQuote();
        var line = AddPainting(quote);
        var version = quote.Version;
        var total = quote.TotalSalesAmount;

        switch (operation)
        {
            case "duplicate":
                Assert.Throws<InvalidOperationException>(() => quote.AddLine(
                    line.Id, "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m)));
                break;
            case "invalid-addition":
                Assert.Throws<ArgumentOutOfRangeException>(() => AddPainting(quote, price: -1m));
                break;
            case "null-quantity":
                Assert.Throws<ArgumentNullException>(() => quote.ChangeLineQuantity(line.Id, null!));
                break;
            case "negative-price":
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => quote.ChangeLineSalesUnitPrice(line.Id, new Money(-1m)));
                break;
            case "missing-quantity-line":
                Assert.Throws<KeyNotFoundException>(
                    () => quote.ChangeLineQuantity(Guid.NewGuid(), new Quantity(120m)));
                break;
            case "missing-price-line":
                Assert.Throws<KeyNotFoundException>(
                    () => quote.ChangeLineSalesUnitPrice(Guid.NewGuid(), new Money(22m)));
                break;
            case "missing-removal-line":
                Assert.Throws<KeyNotFoundException>(() => quote.RemoveLine(Guid.NewGuid()));
                break;
            case "empty-removal-id":
                Assert.Throws<ArgumentException>(() => quote.RemoveLine(Guid.Empty));
                break;
            default:
                throw new ArgumentException("Unknown test operation.", nameof(operation));
        }

        Assert.Equal(version, quote.Version);
        Assert.Same(line, Assert.Single(quote.Lines));
        Assert.Same(total, quote.TotalSalesAmount);
    }

    [Fact]
    public void Amount_overflow_does_not_advance_version()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote, quantity: 1m, price: decimal.MaxValue);
        var version = quote.Version;
        var total = quote.TotalSalesAmount;

        Assert.Throws<OverflowException>(() => AddPainting(quote, quantity: 1m, price: 1m));
        Assert.Throws<OverflowException>(() => quote.ChangeLineQuantity(line.Id, new Quantity(2m)));

        Assert.Equal(version, quote.Version);
        Assert.Same(line, Assert.Single(quote.Lines));
        Assert.Same(total, quote.TotalSalesAmount);
    }

    [Fact]
    public void Repeated_removal_does_not_advance_version_again()
    {
        var quote = CreateQuote();
        var line = AddPainting(quote);
        quote.RemoveLine(line.Id);
        var version = quote.Version;

        Assert.Throws<KeyNotFoundException>(() => quote.RemoveLine(line.Id));

        Assert.Equal(version, quote.Version);
        Assert.Empty(quote.Lines);
    }

    [Fact]
    public void Reverting_a_quantity_is_a_new_revision_and_other_quotes_are_independent()
    {
        var quote = CreateQuote();
        var other = CreateQuote();
        var line = AddPainting(quote);
        AddPainting(other);

        quote.ChangeLineQuantity(line.Id, new Quantity(120m));
        quote.ChangeLineQuantity(line.Id, new Quantity(100m));

        Assert.Equal(4L, quote.Version);
        Assert.Equal(2L, other.Version);
        Assert.Equal(new Quantity(100m), Assert.Single(quote.Lines).Quantity);
    }

    private static Quote CreateQuote() => new(Guid.NewGuid(), Guid.NewGuid());

    private static QuoteLine AddPainting(Quote quote, decimal quantity = 100m, decimal price = 20m)
    {
        return quote.AddLine(
            Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(quantity), new Money(price));
    }
}
