using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.RemoveQuoteLine;

public sealed class RemoveQuoteLineHandler(IQuoteRepository repository) : IRequestHandler<RemoveQuoteLineCommand, RemoveQuoteLineResult>
{
    public async Task<RemoveQuoteLineResult> Handle(RemoveQuoteLineCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.QuoteId == Guid.Empty || request.LineId == Guid.Empty)
            throw new ArgumentException("Quote and line IDs are required.");
        ArgumentOutOfRangeException.ThrowIfLessThan(request.ExpectedVersion, 1L);

        var quote = await repository.GetByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new KeyNotFoundException("Quote was not found.");
        var originalVersion = quote.Version;
        if (originalVersion != request.ExpectedVersion)
            throw new QuoteConcurrencyException(quote.Id, request.ExpectedVersion);

        quote.RemoveLine(request.LineId);
        await repository.SaveAsync(quote, originalVersion, cancellationToken);
        return new RemoveQuoteLineResult(quote.Id, request.LineId,
            quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency, quote.Version);
    }
}
