using ConstructionBudgeting.Sales.Application.Quotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionBudgeting.Sales.Api;

public sealed class SalesExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // 已知业务错误转换为 HTTP 状态码，其他故障交给统一的 500 处理。
        var (status, title) = exception switch
        {
            QuoteAlreadyExistsException => (StatusCodes.Status409Conflict, "该项目已存在报价"),
            QuoteConcurrencyException => (StatusCodes.Status409Conflict, "报价已变化，请重新查询后再修改"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "未找到报价或报价行"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "请求格式无效"),
            ArgumentException => (StatusCodes.Status400BadRequest, "请求参数无效"),
            OverflowException => (StatusCodes.Status400BadRequest, "输入导致金额或版本超出支持范围"),
            _ => (0, string.Empty)
        };
        if (status == 0) return false;

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }
}
