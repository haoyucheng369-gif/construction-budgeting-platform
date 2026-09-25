using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Application.Quotations;

public interface IQuoteRepository
{
    /// <summary>
    /// Loads a quote for this unit of work, or null if it does not exist.
    /// Implementations must not share mutable aggregates between concurrent requests.
    /// </summary>
    Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists the edited aggregate only if its stored version equals expectedVersion.
    /// The version comparison and write must be atomic. A mismatch, including deletion
    /// after loading, throws QuoteConcurrencyException without writing partial state.
    /// A failed unit of work must be discarded; the edited in-memory quote is not rolled back.
    /// </summary>
    Task SaveAsync(Quote quote, long expectedVersion, CancellationToken cancellationToken);
}
