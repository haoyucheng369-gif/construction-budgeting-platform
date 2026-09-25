using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class QuoteQuantityTests
{
    [Theory]
    [InlineData(120, 2400, 4400)]
    [InlineData(50, 1000, 3000)]
    public void Changing_quantity_preserves_line_identity_and_metadata_and_updates_totals(
        int quantity, int expectedLineAmount, int expectedTotal)
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);
        var other = quote.AddLine(
            Guid.NewGuid(), "FLOOR", "Floor installation", "m2", new Quantity(50m), new Money(40m));

        var updated = quote.ChangeLineQuantity(original.Id, new Quantity(quantity));

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.WorkItemCode, updated.WorkItemCode);
        Assert.Equal(original.Description, updated.Description);
        Assert.Equal(original.Unit, updated.Unit);
        Assert.Equal(original.SalesUnitPrice, updated.SalesUnitPrice);
        Assert.Equal(new Quantity(quantity), updated.Quantity);
        Assert.Equal(new Money(expectedLineAmount), updated.LineAmount);
        Assert.Equal(new Money(expectedTotal), quote.TotalSalesAmount);
        Assert.Collection(quote.Lines,
            line => Assert.Same(updated, line),
            line => Assert.Same(other, line));
        Assert.Equal(new Quantity(100m), original.Quantity);
    }

    [Fact]
    public void Same_quantity_is_a_no_op()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);
        var total = quote.TotalSalesAmount;

        var result = quote.ChangeLineQuantity(original.Id, new Quantity(100.00m));

        Assert.Same(original, result);
        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Same(total, quote.TotalSalesAmount);
    }

    [Fact]
    public void Repeated_changes_replace_the_rounded_line_amount_without_accumulating_rounding_error()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: 1m, price: 0.005m);
        AddPainting(quote, quantity: 1m, price: 0.005m);

        var updated = quote.ChangeLineQuantity(original.Id, new Quantity(3m));

        Assert.Equal(new Money(0.02m), updated.LineAmount);
        Assert.Equal(new Money(0.03m), quote.TotalSalesAmount);

        quote.ChangeLineQuantity(original.Id, new Quantity(1m));

        Assert.Equal(new Money(0.02m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Missing_line_is_rejected_without_changing_the_quote()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<KeyNotFoundException>(() => quote.ChangeLineQuantity(Guid.NewGuid(), new Quantity(120m)));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Empty_line_id_is_rejected_without_changing_the_quote()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<ArgumentException>(() => quote.ChangeLineQuantity(Guid.Empty, new Quantity(120m)));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Null_quantity_is_rejected_without_changing_the_quote()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<ArgumentNullException>(() => quote.ChangeLineQuantity(original.Id, null!));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Line_amount_overflow_leaves_quantity_and_total_unchanged()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: 1m, price: decimal.MaxValue);

        Assert.Throws<OverflowException>(() => quote.ChangeLineQuantity(original.Id, new Quantity(2m)));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Quantity(1m), original.Quantity);
        Assert.Equal(new Money(decimal.MaxValue), quote.TotalSalesAmount);
    }

    [Fact]
    public void Total_overflow_leaves_all_lines_and_total_unchanged()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: 1m, price: 1m);
        var other = AddPainting(quote, quantity: 1m, price: decimal.MaxValue - 1m);

        Assert.Throws<OverflowException>(() => quote.ChangeLineQuantity(original.Id, new Quantity(2m)));

        Assert.Collection(quote.Lines,
            line => Assert.Same(original, line),
            line => Assert.Same(other, line));
        Assert.Equal(new Money(decimal.MaxValue), quote.TotalSalesAmount);
    }

    [Fact]
    public void Changing_a_line_does_not_affect_another_quote_using_the_same_line_id()
    {
        var first = CreateQuote();
        var second = CreateQuote();
        var original = AddPainting(first);
        var other = second.AddLine(
            original.Id, "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));

        first.ChangeLineQuantity(original.Id, new Quantity(120m));

        Assert.Equal(new Money(2400m), first.TotalSalesAmount);
        Assert.Same(other, Assert.Single(second.Lines));
        Assert.Equal(new Quantity(100m), other.Quantity);
        Assert.Equal(new Money(2000m), second.TotalSalesAmount);
    }

    [Fact]
    public void Free_line_quantity_can_change_even_when_the_amount_remains_zero()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, price: 0m);

        var updated = quote.ChangeLineQuantity(original.Id, new Quantity(120m));

        Assert.Equal(new Quantity(120m), updated.Quantity);
        Assert.Equal(new Money(0m), updated.LineAmount);
        Assert.Equal(new Money(0m), quote.TotalSalesAmount);
    }

    private static Quote CreateQuote() => new(Guid.NewGuid(), Guid.NewGuid());

    private static QuoteLine AddPainting(Quote quote, decimal quantity = 100m, decimal price = 20m)
    {
        return quote.AddLine(
            Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(quantity), new Money(price));
    }
}
