using System.Globalization;
using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class QuantityTests
{
    [Theory]
    [InlineData("100")]
    [InlineData("12.5")]
    [InlineData("0.0001")]
    public void Positive_quantities_preserve_the_value_without_rounding(string input)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);

        var quantity = new Quantity(value);

        Assert.Equal(value, quantity.Value);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-0.0001")]
    public void Zero_and_negative_quantities_are_rejected(string input)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(value));
    }

    [Fact]
    public void Equal_values_are_equal_even_when_decimal_scales_differ()
    {
        var first = new Quantity(12.5m);
        var second = new Quantity(12.50m);

        Assert.NotSame(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Different_values_are_not_equal()
    {
        Assert.NotEqual(new Quantity(100m), new Quantity(120m));
    }
}
