using ConstructionBudgeting.Sales.Application.Quotations;
using ConstructionBudgeting.Sales.Domain.Quotations;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

public sealed class EfQuoteRepository(IDbContextFactory<SalesDbContext> contextFactory) : IQuoteRepository
{
    // 按 ID 读取报价及全部报价行；找不到则返回 null。
    public async Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ReadQuotes().AsSingleQuery()
            .SingleOrDefaultAsync(quote => quote.Id == quoteId, cancellationToken);
    }

    // 保存修改后的报价，expectedVersion 是修改前的版本。
    public async Task SaveAsync(Quote quote, long expectedVersion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(quote);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedVersion, 1);
        if (quote.Version <= expectedVersion)
        {
            throw new ArgumentException("Only an edited quote with a newer version can be saved.", nameof(quote));
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        // 开启事务：以下修改要么全部成功，要么全部回滚。
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // 只有数据库版本仍等于旧版本时才更新；否则报并发冲突。
        var affected = await context.Quotes
            .Where(stored => stored.Id == quote.Id && stored.Version == expectedVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(stored => stored.Version, quote.Version)
                .SetProperty(stored => stored.TotalSalesAmount, quote.TotalSalesAmount), cancellationToken);
        if (affected == 0)
        {
            throw new QuoteConcurrencyException(quote.Id, expectedVersion);
        }

        // 删除这份报价的旧行，按当前顺序重新插入，保留行 ID。
        await context.Set<QuoteLine>()
            .Where(line => EF.Property<Guid>(line, "QuoteId") == quote.Id)
            .ExecuteDeleteAsync(cancellationToken);
        for (var position = 0; position < quote.Lines.Count; position++)
        {
            var entry = context.Entry(quote.Lines[position]);
            entry.Property<Guid>("QuoteId").CurrentValue = quote.Id;
            entry.Property<int>("Position").CurrentValue = position;
            entry.State = EntityState.Added;
        }

        // 写入报价行，再提交整份报价的修改。
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
