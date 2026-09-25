using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.GetQuote;

public sealed class GetQuoteHandler(IQuoteRepository repository)
    : IRequestHandler<GetQuoteQuery, GetQuoteResult>
{
    public async Task<GetQuoteResult> Handle(GetQuoteQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.QuoteId == Guid.Empty)
        {
            throw new ArgumentException("A quote must have an ID.", nameof(request.QuoteId));
        }

        var quote = await repository.GetByIdAsync(request.QuoteId, cancellationToken)
            ?? throw new KeyNotFoundException($"Quote '{request.QuoteId}' was not found.");

        // Materialize a detached snapshot; amounts are copied, not recalculated here.
        var lines = quote.Lines.Select(line => new QuoteLineDetails(
            line.Id, line.WorkItemCode, line.Description, line.Unit,
            line.Quantity.Value, line.SalesUnitPrice.Amount, line.LineAmount.Amount)).ToArray();

        return new GetQuoteResult(
            quote.Id, quote.ProjectId, quote.Version,
            quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency,
            Array.AsReadOnly(lines));
    }
}
