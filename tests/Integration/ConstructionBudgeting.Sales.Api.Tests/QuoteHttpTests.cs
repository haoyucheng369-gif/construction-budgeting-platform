using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConstructionBudgeting.Sales.Application.Quotations.GetQuote;
using ConstructionBudgeting.Sales.Domain.Quotations;
using ConstructionBudgeting.Sales.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Api.Tests;

public sealed class QuoteHttpTests
{
    [Fact]
    public async Task Swagger_describes_query_and_quantity_edit_in_development()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/swagger/index.html");
        Assert.Contains("swagger-ui", html);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/quotes/{id}").GetProperty("get").GetProperty("responses").TryGetProperty("200", out _));
        var edit = paths.GetProperty("/quotes/{quoteId}/lines/{lineId}/quantity").GetProperty("patch");
        Assert.True(edit.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(edit.TryGetProperty("requestBody", out _));
    }

    [Fact]
    public async Task Swagger_is_not_served_outside_development()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }

    [PostgresApiFact]
    public async Task Query_edit_reload_and_stale_save_use_real_database()
    {
        var quote = await SeedAsync();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var path = $"/quotes/{quote.Id}";
            var editPath = $"{path}/lines/{quote.Lines[0].Id}/quantity";
            var before = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(2049.98m, before.TotalSalesAmount);
            Assert.Equal(quote.ProjectId, before.ProjectId);
            Assert.Equal(quote.Lines.Select(line => line.Id), before.Lines.Select(line => line.LineId));

            var response = await client.PatchAsJsonAsync(editPath, new { quantity = 120m, expectedVersion = before.Version });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var after = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(2449.98m, after.TotalSalesAmount);
            Assert.Equal(120m, after.Lines[0].Quantity);
            Assert.Equal(before.Version + 1, after.Version);

            var conflict = await client.PatchAsJsonAsync(editPath, new { quantity = 130m, expectedVersion = before.Version });
            await AssertProblemAsync(conflict, HttpStatusCode.Conflict);
            var noOp = await client.PatchAsJsonAsync(editPath, new { quantity = 120m, expectedVersion = after.Version });
            Assert.Equal(HttpStatusCode.OK, noOp.StatusCode);

            await using var context = CreateContext();
            var stored = await context.ReadQuotes().SingleAsync(item => item.Id == quote.Id);
            Assert.Equal(after.Version, stored.Version);
            Assert.Equal(new Money(2449.98m), stored.TotalSalesAmount);
            Assert.Equal(new Quantity(120m), stored.Lines[0].Quantity);
        }
        finally { await CleanupAsync(quote.Id); }
    }

    [PostgresApiFact]
    public async Task Invalid_inputs_and_missing_lines_return_problems_without_writes()
    {
        var quote = await SeedAsync();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var editPath = $"/quotes/{quote.Id}/lines/{quote.Lines[0].Id}/quantity";
            foreach (var input in new[] { (0m, quote.Version), (-1m, quote.Version), (120m, 0L), (decimal.MaxValue, quote.Version) })
            {
                await AssertProblemAsync(await client.PatchAsJsonAsync(editPath,
                    new { quantity = input.Item1, expectedVersion = input.Item2 }), HttpStatusCode.BadRequest);
            }
            await AssertProblemAsync(await client.PatchAsJsonAsync(editPath, new { quantity = 120m }), HttpStatusCode.BadRequest);
            using var malformed = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
            await AssertProblemAsync(await client.PatchAsync(editPath, malformed), HttpStatusCode.BadRequest);
            await AssertProblemAsync(await client.PatchAsJsonAsync($"/quotes/{quote.Id}/lines/{Guid.NewGuid()}/quantity",
                new { quantity = 120m, expectedVersion = quote.Version }), HttpStatusCode.NotFound);
            await AssertProblemAsync(await client.GetAsync($"/quotes/{Guid.Empty}"), HttpStatusCode.BadRequest);

            await using var context = CreateContext();
            var stored = await context.ReadQuotes().SingleAsync(item => item.Id == quote.Id);
            Assert.Equal(quote.Version, stored.Version);
            Assert.Equal(quote.TotalSalesAmount, stored.TotalSalesAmount);
        }
        finally { await CleanupAsync(quote.Id); }
    }

    [PostgresApiFact]
    public async Task Missing_quote_returns_404_for_query_and_edit()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        await AssertProblemAsync(await client.GetAsync($"/quotes/{id}"), HttpStatusCode.NotFound);
        await AssertProblemAsync(await client.PatchAsJsonAsync($"/quotes/{id}/lines/{Guid.NewGuid()}/quantity",
            new { quantity = 120m, expectedVersion = 3L }), HttpStatusCode.NotFound);
    }

    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    private static SalesDbContext CreateContext() => new SalesDbContextFactory().CreateDbContext([]);

    private static async Task<Quote> SeedAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid());
        quote.AddLine(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), "PAINT", "墙面涂装", "m2", new Quantity(100m), new Money(20m));
        quote.AddLine(Guid.Parse("00000000-0000-0000-0000-000000000001"), "FLOOR", "地板铺设", "m2", new Quantity(2.5m), new Money(19.99m));
        context.Quotes.Add(quote);
        await context.SaveChangesAsync();
        return quote;
    }

    private static async Task CleanupAsync(Guid quoteId)
    {
        await using var context = CreateContext();
        // 只清理测试自己的随机报价，保留手动操作的示例报价。
        await context.Quotes.Where(quote => quote.Id == quoteId).ExecuteDeleteAsync();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, body.RootElement.GetProperty("status").GetInt32());
    }
}

public sealed class PostgresApiFactAttribute : FactAttribute
{
    public PostgresApiFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SALES_PERSISTENCE_TESTS") != "1")
            Skip = "需要 PostgreSQL；请运行 scripts/Test-SalesPersistence.ps1。";
    }
}
