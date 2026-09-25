using ConstructionBudgeting.Sales.Domain.Quotations;
using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;

public sealed class ChangeQuoteLineQuantityHandler(IQuoteRepository repository)
    : IRequestHandler<ChangeQuoteLineQuantityCommand, ChangeQuoteLineQuantityResult>
{
    public async Task<ChangeQuoteLineQuantityResult> Handle(
        ChangeQuoteLineQuantityCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.QuoteId == Guid.Empty)
        {
            throw new ArgumentException("A quote must have an ID.", nameof(request.QuoteId));
        }

        if (request.LineId == Guid.Empty)
        {
            throw new ArgumentException("A quote line must have an ID.", nameof(request.LineId));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(request.ExpectedVersion, 1L);
        var quantity = new Quantity(request.Quantity);

        var quote = await repository.GetByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new KeyNotFoundException($"Quote '{request.QuoteId}' was not found.");

        // Reject stale input before modifying the loaded aggregate, including no-op requests.
        var originalVersion = quote.Version;
        if (originalVersion != request.ExpectedVersion)
        {
            throw new QuoteConcurrencyException(quote.Id, request.ExpectedVersion);
        }

        var line = quote.ChangeLineQuantity(request.LineId, quantity);
        if (quote.Version != originalVersion)
        {
            // The repository must also check atomically: another writer may save after our read.
            await repository.SaveAsync(quote, originalVersion, cancellationToken);
        }

        return new ChangeQuoteLineQuantityResult(
            quote.Id, line.Id, line.Quantity.Value, line.LineAmount.Amount,
            quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency, quote.Version);
    }
}
