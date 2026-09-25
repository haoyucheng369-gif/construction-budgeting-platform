using ConstructionBudgeting.Sales.Api;
using ConstructionBudgeting.Sales.Application;
using ConstructionBudgeting.Sales.Application.Quotations;
using ConstructionBudgeting.Sales.Infrastructure.Persistence;
using ConstructionBudgeting.Sales.Api.Swagger;
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
builder.Services.AddSwaggerGen(options => options.OperationFilter<SalesExamplesOperationFilter>());

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

app.MapQuoteEndpoints();

// 当前仅检查进程存活；数据库和消息服务的就绪检查随相应功能接入。
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
