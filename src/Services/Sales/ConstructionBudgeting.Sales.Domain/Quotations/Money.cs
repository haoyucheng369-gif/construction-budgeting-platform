namespace ConstructionBudgeting.Sales.Domain.Quotations;

/// <summary>
/// An immutable EUR amount. Sign constraints belong to the business operation
/// using the amount; rounding is explicit to preserve unit-price precision.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "EUR")
    {
        if (currency != "EUR")
        {
            throw new ArgumentException("Only EUR is supported.", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    public Money RoundToCents()
    {
        return new Money(decimal.Round(Amount, 2, MidpointRounding.AwayFromZero), Currency);
    }
}
