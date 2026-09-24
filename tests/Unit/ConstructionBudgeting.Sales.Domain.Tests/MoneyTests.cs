using System.Globalization;
using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("20.1234")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("-0.0001")]
    public void Amounts_preserve_precision_and_allow_zero_and_negative_values(string input)
    {
        var amount = decimal.Parse(input, CultureInfo.InvariantCulture);

        var money = new Money(amount);

        Assert.Equal(amount, money.Amount);
        Assert.Equal("EUR", money.Currency);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("USD")]
    [InlineData("eur")]
    public void Unsupported_or_missing_currency_is_rejected(string? currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(20m, currency!));
    }

    [Theory]
    [InlineData("1.005", "1.01")]
    [InlineData("-1.005", "-1.01")]
    [InlineData("1.004", "1.00")]
    [InlineData("-1.004", "-1.00")]
    [InlineData("0.005", "0.01")]
    [InlineData("-0.005", "-0.01")]
    public void Rounding_to_cents_uses_away_from_zero_without_changing_the_original(
        string input, string expected)
    {
        var amount = decimal.Parse(input, CultureInfo.InvariantCulture);
        var money = new Money(amount);

        var rounded = money.RoundToCents();

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), rounded.Amount);
        Assert.Equal("EUR", rounded.Currency);
        Assert.Equal(amount, money.Amount);
    }

    [Fact]
    public void Unit_price_precision_is_preserved_until_the_final_amount_is_rounded()
    {
        var unitPrice = new Money(1.005m);
        var quantity = new Quantity(100m);

        var total = new Money(unitPrice.Amount * quantity.Value).RoundToCents();

        Assert.Equal(100.50m, total.Amount);
    }

    [Fact]
    public void Equal_amounts_and_currency_have_value_equality()
    {
        var first = new Money(20m);
        var second = new Money(20.00m, "EUR");

        Assert.NotSame(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Amounts_that_only_become_equal_after_rounding_are_initially_different()
    {
        var first = new Money(1.001m);
        var second = new Money(1.002m);

        Assert.NotEqual(first, second);
        Assert.Equal(first.RoundToCents(), second.RoundToCents());
    }
}
