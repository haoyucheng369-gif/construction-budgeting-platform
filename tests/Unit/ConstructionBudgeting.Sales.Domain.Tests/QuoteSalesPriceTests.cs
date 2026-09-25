using System.Globalization;
using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class QuoteSalesPriceTests
{
    [Theory]
    [InlineData(22, 2640, 4640)]
    [InlineData(10, 1200, 3200)]
    [InlineData(0, 0, 2000)]
    public void Price_changes_preserve_quantity_identity_and_metadata_and_update_totals(
        int price, int expectedLineAmount, int expectedTotal)
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);
        var other = quote.AddLine(
            Guid.NewGuid(), "FLOOR", "Floor installation", "m2", new Quantity(50m), new Money(40m));

        var updated = quote.ChangeLineSalesUnitPrice(original.Id, new Money(price));

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.WorkItemCode, updated.WorkItemCode);
        Assert.Equal(original.Description, updated.Description);
        Assert.Equal(original.Unit, updated.Unit);
        Assert.Equal(original.Quantity, updated.Quantity);
        Assert.Equal(new Money(price), updated.SalesUnitPrice);
        Assert.Equal(new Money(expectedLineAmount), updated.LineAmount);
        Assert.Equal(new Money(expectedTotal), quote.TotalSalesAmount);
        Assert.Collection(quote.Lines,
            line => Assert.Same(updated, line),
            line => Assert.Same(other, line));
        Assert.Equal(new Money(20m), original.SalesUnitPrice);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-0.0001")]
    public void Negative_price_is_rejected_before_rounding_without_changing_state(string input)
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<ArgumentOutOfRangeException>(() => quote.ChangeLineSalesUnitPrice(
            original.Id, new Money(decimal.Parse(input, CultureInfo.InvariantCulture))));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2400m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Same_price_is_a_no_op()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);
        var total = quote.TotalSalesAmount;

        var result = quote.ChangeLineSalesUnitPrice(original.Id, new Money(20.00m));

        Assert.Same(original, result);
        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Same(total, quote.TotalSalesAmount);
    }

    [Theory]
    [InlineData("100", "1.005", "100.50")]
    [InlineData("12.5", "19.99", "249.88")]
    public void Changed_price_is_not_rounded_before_multiplication(
        string quantityInput, string priceInput, string expectedInput)
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: decimal.Parse(quantityInput, CultureInfo.InvariantCulture));
        var price = new Money(decimal.Parse(priceInput, CultureInfo.InvariantCulture));

        var updated = quote.ChangeLineSalesUnitPrice(original.Id, price);

        Assert.Equal(price, updated.SalesUnitPrice);
        Assert.Equal(new Money(decimal.Parse(expectedInput, CultureInfo.InvariantCulture)), updated.LineAmount);
        Assert.Equal(updated.LineAmount, quote.TotalSalesAmount);
    }

    [Fact]
    public void Price_is_updated_even_when_rounded_line_amount_does_not_change()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: 1m, price: 1.001m);

        var updated = quote.ChangeLineSalesUnitPrice(original.Id, new Money(1.002m));

        Assert.Equal(new Money(1.002m), updated.SalesUnitPrice);
        Assert.Equal(original.LineAmount, updated.LineAmount);
        Assert.Equal(new Money(1m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Null_price_is_rejected_without_changing_state()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<ArgumentNullException>(() => quote.ChangeLineSalesUnitPrice(original.Id, null!));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2400m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Missing_line_is_rejected_without_changing_state()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<KeyNotFoundException>(() => quote.ChangeLineSalesUnitPrice(Guid.NewGuid(), new Money(22m)));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2400m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Empty_line_id_is_rejected_without_changing_state()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<ArgumentException>(() => quote.ChangeLineSalesUnitPrice(Guid.Empty, new Money(22m)));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2400m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Line_overflow_leaves_price_and_total_unchanged()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<OverflowException>(() => quote.ChangeLineSalesUnitPrice(original.Id, new Money(decimal.MaxValue)));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2400m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Total_overflow_leaves_all_lines_and_total_unchanged()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: 1m, price: 1m);
        var other = AddPainting(quote, quantity: 1m, price: decimal.MaxValue - 1m);

        Assert.Throws<OverflowException>(() => quote.ChangeLineSalesUnitPrice(original.Id, new Money(2m)));

        Assert.Collection(quote.Lines,
            line => Assert.Same(original, line),
            line => Assert.Same(other, line));
        Assert.Equal(new Money(decimal.MaxValue), quote.TotalSalesAmount);
    }

    [Fact]
    public void Interleaved_quantity_and_price_changes_use_the_latest_line_values()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: 100m);
        quote.ChangeLineQuantity(original.Id, new Quantity(120m));

        quote.ChangeLineSalesUnitPrice(original.Id, new Money(22m));
        Assert.Equal(new Money(2640m), quote.TotalSalesAmount);

        var updated = quote.ChangeLineQuantity(original.Id, new Quantity(100m));
        Assert.Equal(new Money(22m), updated.SalesUnitPrice);
        Assert.Equal(new Money(2200m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Price_change_does_not_affect_another_quote_with_the_same_line_id()
    {
        var first = CreateQuote();
        var second = CreateQuote();
        var original = AddPainting(first);
        var other = second.AddLine(
            original.Id, "PAINT", "Wall painting", "m2", new Quantity(120m), new Money(20m));

        first.ChangeLineSalesUnitPrice(original.Id, new Money(22m));

        Assert.Equal(new Money(2640m), first.TotalSalesAmount);
        Assert.Same(other, Assert.Single(second.Lines));
        Assert.Equal(new Money(2400m), second.TotalSalesAmount);
    }

    private static Quote CreateQuote() => new(Guid.NewGuid(), Guid.NewGuid());

    private static QuoteLine AddPainting(Quote quote, decimal quantity = 120m, decimal price = 20m)
    {
        return quote.AddLine(
            Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(quantity), new Money(price));
    }
}
