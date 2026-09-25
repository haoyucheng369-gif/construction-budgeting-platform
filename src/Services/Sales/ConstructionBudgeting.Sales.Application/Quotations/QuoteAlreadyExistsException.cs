namespace ConstructionBudgeting.Sales.Application.Quotations;

public sealed class QuoteAlreadyExistsException(Guid projectId, Exception? innerException = null)
    : Exception($"Project '{projectId}' already has a quotation.", innerException)
{
    public Guid ProjectId { get; } = projectId;
}
