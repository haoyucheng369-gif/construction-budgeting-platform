namespace ConstructionBudgeting.Sales.Domain.Quotations;

/// <summary>
/// 不可变的欧元金额。是否允许负数由使用金额的具体业务操作决定；
/// 舍入需要显式调用，避免提前丢失单价精度。
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
