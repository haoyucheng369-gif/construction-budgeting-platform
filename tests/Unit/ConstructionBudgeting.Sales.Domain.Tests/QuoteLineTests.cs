using System.Globalization;
using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class QuoteLineTests
{
    [Fact]
    public void Painting_line_combines_work_item_quantity_and_price()
    {
        var id = Guid.NewGuid();

        var line = CreateLine(id: id);

        Assert.Equal(id, line.Id);
        Assert.Equal("PAINT", line.WorkItemCode);
        Assert.Equal("Wall painting", line.Description);
        Assert.Equal("m2", line.Unit);
        Assert.Equal(new Quantity(100m), line.Quantity);
        Assert.Equal(new Money(20m), line.SalesUnitPrice);
        Assert.Equal(new Money(2000m), line.LineAmount);
    }

    [Fact]
    public void Zero_sales_price_is_allowed()
    {
        var line = CreateLine(price: 0m);

        Assert.Equal(new Money(0m), line.LineAmount);
    }

    [Theory]
    [InlineData("12.5", "19.99", "249.88")]
    [InlineData("100", "1.005", "100.50")]
    [InlineData("1", "0.005", "0.01")]
    [InlineData("0.0001", "10000", "1.00")]
    public void Line_amount_is_rounded_after_multiplication_without_rounding_inputs(
        string quantityInput, string priceInput, string expectedInput)
    {
        var quantity = decimal.Parse(quantityInput, CultureInfo.InvariantCulture);
        var price = decimal.Parse(priceInput, CultureInfo.InvariantCulture);

        var line = CreateLine(quantity: quantity, price: price);

        Assert.Equal(decimal.Parse(expectedInput, CultureInfo.InvariantCulture), line.LineAmount.Amount);
        Assert.Equal(quantity, line.Quantity.Value);
        Assert.Equal(price, line.SalesUnitPrice.Amount);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-0.0001")]
    public void Negative_sales_price_is_rejected_even_if_it_would_round_to_zero(string input)
    {
        var price = decimal.Parse(input, CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateLine(price: price));
    }

    [Fact]
    public void Empty_line_identity_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => CreateLine(id: Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void Missing_work_item_description_or_unit_is_rejected(string? input)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateLine(workItemCode: input!));
        Assert.ThrowsAny<ArgumentException>(() => CreateLine(description: input!));
        Assert.ThrowsAny<ArgumentException>(() => CreateLine(unit: input!));
    }

    [Fact]
    public void Null_quantity_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new QuoteLine(
            Guid.NewGuid(), "PAINT", "Wall painting", "m2", null!, new Money(20m)));
    }

    [Fact]
    public void Null_sales_price_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new QuoteLine(
            Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(100m), null!));
    }

    [Fact]
    public void Surrounding_whitespace_is_removed_from_work_item_metadata()
    {
        var line = CreateLine(workItemCode: " PAINT ", description: " Wall painting ", unit: " m2 ");

        Assert.Equal("PAINT", line.WorkItemCode);
        Assert.Equal("Wall painting", line.Description);
        Assert.Equal("m2", line.Unit);
    }

    [Fact]
    public void Identical_work_items_can_have_distinct_line_identities()
    {
        var first = CreateLine();
        var second = CreateLine();

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.WorkItemCode, second.WorkItemCode);
        Assert.Equal(first.LineAmount, second.LineAmount);
    }

    [Fact]
    public void Unrepresentable_line_amount_fails_instead_of_being_clamped()
    {
        Assert.Throws<OverflowException>(() => CreateLine(quantity: 2m, price: decimal.MaxValue));
    }

    private static QuoteLine CreateLine(
        Guid? id = null,
        string workItemCode = "PAINT",
        string description = "Wall painting",
        string unit = "m2",
        decimal quantity = 100m,
        decimal price = 20m)
    {
        return new QuoteLine(
            id ?? Guid.NewGuid(), workItemCode, description, unit,
            new Quantity(quantity), new Money(price));
    }
}
