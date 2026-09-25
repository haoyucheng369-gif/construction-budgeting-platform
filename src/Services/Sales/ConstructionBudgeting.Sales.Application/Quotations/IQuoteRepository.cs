using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Application.Quotations;

public interface IQuoteRepository
{
    /// <summary>
    /// 为本次业务操作加载报价；不存在时返回 null。
    /// 实现时不能让并发请求共享同一个可变的聚合对象。
    /// </summary>
    Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken);

    /// <summary>
    /// 仅在数据库版本等于 expectedVersion 时，保存已修改的聚合。
    /// 版本比较和写入必须保证原子性。版本不匹配（包括加载后报价被删除）时，
    /// 抛出 QuoteConcurrencyException，不得留下部分写入的数据。
    /// 保存失败后应丢弃本次业务操作的对象；内存中已修改的报价不会自动回滚。
    /// </summary>
    Task SaveAsync(Quote quote, long expectedVersion, CancellationToken cancellationToken);
}
