using ConstructionBudgeting.Sales.Domain.Quotations;
using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineSalesUnitPrice;

public sealed class ChangeQuoteLineSalesUnitPriceHandler(IQuoteRepository repository)
    : IRequestHandler<ChangeQuoteLineSalesUnitPriceCommand, ChangeQuoteLineSalesUnitPriceResult>
{
    public async Task<ChangeQuoteLineSalesUnitPriceResult> Handle(
        ChangeQuoteLineSalesUnitPriceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.QuoteId == Guid.Empty || request.LineId == Guid.Empty)
            throw new ArgumentException("Quote and line IDs are required.");
        ArgumentOutOfRangeException.ThrowIfLessThan(request.ExpectedVersion, 1L);
        var price = new Money(request.SalesUnitPrice);

        var quote = await repository.GetByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new KeyNotFoundException("Quote was not found.");
        var originalVersion = quote.Version;
        if (originalVersion != request.ExpectedVersion)
            throw new QuoteConcurrencyException(quote.Id, request.ExpectedVersion);

        var line = quote.ChangeLineSalesUnitPrice(request.LineId, price);
        if (quote.Version != originalVersion)
            await repository.SaveAsync(quote, originalVersion, cancellationToken);
        return new ChangeQuoteLineSalesUnitPriceResult(quote.Id, line.Id, line.SalesUnitPrice.Amount,
            line.LineAmount.Amount, quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency, quote.Version);
    }
}
