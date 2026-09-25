namespace ConstructionBudgeting.Sales.Domain.Quotations;

/// <summary>
/// 严格大于零的工程量，其计量单位由所属工程项指定。
/// </summary>
public sealed record Quantity
{
    public decimal Value { get; }

    public Quantity(decimal value)
    {
        if (value <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, "Quantity must be greater than zero.");
        }

        Value = value;
    }
}
