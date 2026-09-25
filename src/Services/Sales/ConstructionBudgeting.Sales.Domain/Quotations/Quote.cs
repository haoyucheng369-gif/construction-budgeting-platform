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
}
