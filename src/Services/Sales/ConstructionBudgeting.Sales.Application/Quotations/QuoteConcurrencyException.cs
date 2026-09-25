namespace ConstructionBudgeting.Sales.Application.Quotations;

public sealed class QuoteConcurrencyException : Exception
{
    public Guid QuoteId { get; }
    public long ExpectedVersion { get; }

    public QuoteConcurrencyException(Guid quoteId, long expectedVersion)
        : base($"Quote '{quoteId}' no longer matches expected version {expectedVersion}. Reload before editing.")
    {
        QuoteId = quoteId;
        ExpectedVersion = expectedVersion;
    }
}
