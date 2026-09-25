using ConstructionBudgeting.Sales.Domain.Quotations;
using MediatR;

namespace ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;

public sealed class CreateQuoteHandler(IQuoteRepository repository) : IRequestHandler<CreateQuoteCommand, CreateQuoteResult>
{
    public async Task<CreateQuoteResult> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var quote = new Quote(Guid.NewGuid(), request.ProjectId);
        await repository.AddAsync(quote, cancellationToken);
        return new CreateQuoteResult(quote.Id, quote.ProjectId, quote.Version,
            quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency);
    }
}
