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

        // 修改已加载的聚合前，先拒绝过期版本；即使数量没有变化，也要检查版本。
        var originalVersion = quote.Version;
        if (originalVersion != request.ExpectedVersion)
        {
            throw new QuoteConcurrencyException(quote.Id, request.ExpectedVersion);
        }

        var line = quote.ChangeLineQuantity(request.LineId, quantity);
        if (quote.Version != originalVersion)
        {
            // 读取后仍可能有人抢先保存，因此仓储必须在数据库写入时再次原子检查版本。
            await repository.SaveAsync(quote, originalVersion, cancellationToken);
        }

        return new ChangeQuoteLineQuantityResult(
            quote.Id, line.Id, line.Quantity.Value, line.LineAmount.Amount,
            quote.TotalSalesAmount.Amount, quote.TotalSalesAmount.Currency, quote.Version);
    }
}
