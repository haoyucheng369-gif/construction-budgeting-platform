using ConstructionBudgeting.Sales.Domain.Quotations;
using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.AddQuoteLine;

public sealed class AddQuoteLineHandler(IQuoteRepository repository) : IRequestHandler<AddQuoteLineCommand, AddQuoteLineResult>
{
    public async Task<AddQuoteLineResult> Handle(AddQuoteLineCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.QuoteId == Guid.Empty) throw new ArgumentException("A quote must have an ID.");
        ArgumentOutOfRangeException.ThrowIfLessThan(request.ExpectedVersion, 1L);

        var quote = await repository.GetByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new KeyNotFoundException("Quote was not found.");
        var originalVersion = quote.Version;
        if (originalVersion != request.ExpectedVersion)
            throw new QuoteConcurrencyException(quote.Id, request.ExpectedVersion);

        var line = quote.AddLine(Guid.NewGuid(), request.WorkItemCode, request.Description, request.Unit,
            new Quantity(request.Quantity), new Money(request.SalesUnitPrice));
        await repository.SaveAsync(quote, originalVersion, cancellationToken);
        return new AddQuoteLineResult(quote.Id, line.Id, line.Quantity.Value, line.SalesUnitPrice.Amount,
            line.LineAmount.Amount, quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency, quote.Version);
    }
}
