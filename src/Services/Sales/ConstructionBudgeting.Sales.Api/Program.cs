using ConstructionBudgeting.Sales.Api;
using ConstructionBudgeting.Sales.Application;
using ConstructionBudgeting.Sales.Application.Quotations;
using ConstructionBudgeting.Sales.Application.Quotations.GetQuote;
using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;
using ConstructionBudgeting.Sales.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SalesExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddSalesApplication();
builder.Services.AddDbContextFactory<SalesDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Sales"),
    postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "sales")));
builder.Services.AddScoped<IQuoteRepository, EfQuoteRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 仅显式执行初始化命令时创建示例，正常启动不修改数据库。
if (args.Contains("--seed-sample"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Sample initialization requires Development environment.");
    await SalesSampleData.InitializeAsync(app.Services.GetRequiredService<IDbContextFactory<SalesDbContext>>());
    Console.WriteLine($"Sample quote: {SalesSampleData.QuoteId}");
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTP 入口转发给已有用例，业务计算和数据库读写仍由内层负责。
app.MapGet("/quotes/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
    TypedResults.Ok(await sender.Send(new GetQuoteQuery(id), cancellationToken)))
    .WithName("GetQuote")
    .WithTags("报价")
    .WithSummary("查询报价及全部报价行")
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status404NotFound);

app.MapPatch("/quotes/{quoteId:guid}/lines/{lineId:guid}/quantity",
    async (Guid quoteId, Guid lineId, ChangeQuantityRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ChangeQuoteLineQuantityCommand(
            quoteId, lineId, request.Quantity, request.ExpectedVersion), cancellationToken)))
    .WithName("ChangeQuoteLineQuantity")
    .WithTags("报价")
    .WithSummary("修改一行工程量")
    .WithDescription("quantity 填新数量；expectedVersion 填刚才查询得到的 version。版本过期返回 409，请重新查询。")
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

// 当前仅检查进程存活；数据库和消息服务的就绪检查随相应功能接入。
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
