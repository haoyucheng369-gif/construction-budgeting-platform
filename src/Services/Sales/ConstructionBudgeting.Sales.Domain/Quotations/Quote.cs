namespace ConstructionBudgeting.Sales.Domain.Quotations;

/// <summary>
/// A project quotation that owns its lines and controls changes to its total.
/// </summary>
public sealed class Quote
{
    private readonly List<QuoteLine> _lines = [];

    public Guid Id { get; }
    public Guid ProjectId { get; }
    public IReadOnlyList<QuoteLine> Lines { get; }
    public Money TotalSalesAmount { get; private set; } = new(0m);

    public Quote(Guid id, Guid projectId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A quote must have an ID.", nameof(id));
        }

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("A quote must belong to a project.", nameof(projectId));
        }

        Id = id;
        ProjectId = projectId;
        Lines = _lines.AsReadOnly();
    }

    public QuoteLine AddLine(
        Guid lineId,
        string workItemCode,
        string description,
        string unit,
        Quantity quantity,
        Money salesUnitPrice)
    {
        if (_lines.Any(line => line.Id == lineId))
        {
            throw new InvalidOperationException("The quote already contains this line ID.");
        }

        var line = new QuoteLine(lineId, workItemCode, description, unit, quantity, salesUnitPrice);

        // Validate both the line and the new total before changing aggregate state.
        var newTotal = new Money(TotalSalesAmount.Amount + line.LineAmount.Amount);

        _lines.Add(line);
        TotalSalesAmount = newTotal;

        return line;
    }

    public QuoteLine ChangeLineQuantity(Guid lineId, Quantity quantity)
    {
        if (lineId == Guid.Empty)
        {
            throw new ArgumentException("A quote line must have an ID.", nameof(lineId));
        }

        ArgumentNullException.ThrowIfNull(quantity);

        var index = _lines.FindIndex(line => line.Id == lineId);
        if (index < 0)
        {
            throw new KeyNotFoundException("The quote does not contain this line ID.");
        }

        var original = _lines[index];
        if (original.Quantity == quantity)
        {
            return original;
        }

        var updated = new QuoteLine(
            original.Id, original.WorkItemCode, original.Description, original.Unit,
            quantity, original.SalesUnitPrice);
        var newTotal = new Money(
            TotalSalesAmount.Amount - original.LineAmount.Amount + updated.LineAmount.Amount);

        // Replace only after all validation and calculations have succeeded.
        _lines[index] = updated;
        TotalSalesAmount = newTotal;

        return updated;
    }

    public QuoteLine ChangeLineSalesUnitPrice(Guid lineId, Money salesUnitPrice)
    {
        if (lineId == Guid.Empty)
        {
            throw new ArgumentException("A quote line must have an ID.", nameof(lineId));
        }

        ArgumentNullException.ThrowIfNull(salesUnitPrice);

        var index = _lines.FindIndex(line => line.Id == lineId);
        if (index < 0)
        {
            throw new KeyNotFoundException("The quote does not contain this line ID.");
        }

        var original = _lines[index];
        if (original.SalesUnitPrice == salesUnitPrice)
        {
            return original;
        }

        var updated = new QuoteLine(
            original.Id, original.WorkItemCode, original.Description, original.Unit,
            original.Quantity, salesUnitPrice);
        var newTotal = new Money(
            TotalSalesAmount.Amount - original.LineAmount.Amount + updated.LineAmount.Amount);

        // QuoteLine validates the price and rounds the product before state changes.
        _lines[index] = updated;
        TotalSalesAmount = newTotal;

        return updated;
    }
}
