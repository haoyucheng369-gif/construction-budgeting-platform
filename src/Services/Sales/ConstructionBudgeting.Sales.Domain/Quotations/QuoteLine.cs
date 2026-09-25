namespace ConstructionBudgeting.Sales.Domain.Quotations;

/// <summary>
/// 报价中具有独立行标识的工程项。单价对应 Unit 所表示的计量单位；
/// 先将单价乘以工程量，再对行金额进行舍入。
/// </summary>
public sealed class QuoteLine
{
    public Guid Id { get; }
    public string WorkItemCode { get; }
    public string Description { get; }
    public string Unit { get; }
    public Quantity Quantity { get; }
    public Money SalesUnitPrice { get; }
    public Money LineAmount { get; }

    public QuoteLine(
        Guid id,
        string workItemCode,
        string description,
        string unit,
        Quantity quantity,
        Money salesUnitPrice)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A quote line must have an ID.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(workItemCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        ArgumentNullException.ThrowIfNull(quantity);
        ArgumentNullException.ThrowIfNull(salesUnitPrice);

        if (salesUnitPrice.Amount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(salesUnitPrice), salesUnitPrice.Amount, "Sales unit price cannot be negative.");
        }

        Id = id;
        WorkItemCode = workItemCode.Trim();
        Description = description.Trim();
        Unit = unit.Trim();
        Quantity = quantity;
        SalesUnitPrice = salesUnitPrice;
        LineAmount = new Money(quantity.Value * salesUnitPrice.Amount, salesUnitPrice.Currency)
            .RoundToCents();
    }
}
