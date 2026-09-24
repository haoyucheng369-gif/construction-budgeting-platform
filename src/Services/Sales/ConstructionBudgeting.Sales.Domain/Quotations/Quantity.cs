namespace ConstructionBudgeting.Sales.Domain.Quotations;

/// <summary>
/// A strictly positive quantity, expressed in the work item's unit of measure.
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
