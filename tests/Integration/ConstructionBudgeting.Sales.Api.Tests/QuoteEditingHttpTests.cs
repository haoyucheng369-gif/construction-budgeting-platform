using System.Net;
using System.Net.Http.Json;
using ConstructionBudgeting.Sales.Application.Quotations.AddQuoteLine;
using ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;
using ConstructionBudgeting.Sales.Application.Quotations.GetQuote;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Api.Tests;

public sealed partial class QuoteHttpTests
{
    [PostgresApiFact]
    public async Task Complete_quote_workflow_creates_adds_edits_removes_and_reloads()
    {
        await EnsureDatabaseAsync();
        var projectId = Guid.NewGuid();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var createdResponse = await client.PostAsJsonAsync("/quotes", new { projectId });
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = (await createdResponse.Content.ReadFromJsonAsync<CreateQuoteResult>())!;
            Assert.Equal(projectId, created.ProjectId);
            Assert.NotEqual(Guid.Empty, created.QuoteId);
            Assert.Equal(1L, created.Version);
            Assert.Equal(0m, created.TotalSalesAmount);
            Assert.Equal("EUR", created.Currency);
            var path = $"/quotes/{created.QuoteId}";
            Assert.EndsWith(path, createdResponse.Headers.Location!.ToString());
            Assert.Empty((await client.GetFromJsonAsync<GetQuoteResult>(path))!.Lines);
            await AssertProblemAsync(await client.PostAsJsonAsync("/quotes", new { projectId }), HttpStatusCode.Conflict);

            var first = await AddLineAsync(client, created.QuoteId, "PAINT", 100m, 20m, 1);
            var second = await AddLineAsync(client, created.QuoteId, "FLOOR", 2.5m, 19.99m, 2);
            Assert.NotEqual(first.LineId, second.LineId);
            Assert.Equal(2049.98m, second.TotalSalesAmount);
            Assert.Equal(3L, second.Version);
            var priceResponse = await client.PatchAsJsonAsync($"{path}/lines/{first.LineId}/sales-unit-price",
                new { salesUnitPrice = 22m, expectedVersion = 3 });
            Assert.Equal(HttpStatusCode.OK, priceResponse.StatusCode);
            var quantityResponse = await client.PatchAsJsonAsync($"{path}/lines/{first.LineId}/quantity",
                new { quantity = 120m, expectedVersion = 4 });
            Assert.Equal(HttpStatusCode.OK, quantityResponse.StatusCode);
            var edited = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(2689.98m, edited.TotalSalesAmount);
            Assert.Equal(5L, edited.Version);
            Assert.Equal(new[] { first.LineId, second.LineId }, edited.Lines.Select(line => line.LineId));

            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{path}/lines/{second.LineId}?expectedVersion=5")).StatusCode);
            var oneLine = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(2640m, oneLine.TotalSalesAmount);
            Assert.Equal(first.LineId, Assert.Single(oneLine.Lines).LineId);
            Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{path}/lines/{first.LineId}?expectedVersion=6")).StatusCode);
            var empty = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Empty(empty.Lines);
            Assert.Equal(0m, empty.TotalSalesAmount);
            Assert.Equal(7L, empty.Version);
            var free = await AddLineAsync(client, created.QuoteId, "FREE", 1m, 0m, 7);
            Assert.Equal(8L, free.Version);

            // 新宿主和上下文读取持久化结果，验证数据不依赖进程内对象。
            await using var restarted = CreateFactory();
            using var freshClient = restarted.CreateClient();
            var restored = (await freshClient.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(free.Version, restored.Version);
            Assert.Equal(0m, restored.TotalSalesAmount);
            Assert.Equal(free.LineId, Assert.Single(restored.Lines).LineId);
        }
        finally { await CleanupProjectAsync(projectId); }
    }

    [PostgresApiFact]
    public async Task Concurrent_creation_for_one_project_returns_one_created_and_one_conflict()
    {
        await EnsureDatabaseAsync();
        var projectId = Guid.NewGuid();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var responses = await Task.WhenAll(client.PostAsJsonAsync("/quotes", new { projectId }),
                client.PostAsJsonAsync("/quotes", new { projectId }));
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            await AssertProblemAsync(Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
            await using var context = CreateContext();
            Assert.Single(await context.Quotes.Where(quote => quote.ProjectId == projectId).ToListAsync());
        }
        finally { await CleanupProjectAsync(projectId); }
    }

    [PostgresApiFact]
    public async Task New_edit_routes_reject_stale_versions_and_keep_existing_state()
    {
        var quote = await SeedAsync();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var path = $"/quotes/{quote.Id}";
            var linePath = $"{path}/lines/{quote.Lines[0].Id}";
            await AssertProblemAsync(await client.PostAsJsonAsync($"{path}/lines",
                new { workItemCode = "PAINT", description = "涂装", unit = "m2", quantity = 10m, salesUnitPrice = 20m, expectedVersion = 2 }), HttpStatusCode.Conflict);
            await AssertProblemAsync(await client.PatchAsJsonAsync($"{linePath}/sales-unit-price",
                new { salesUnitPrice = 20m, expectedVersion = 2 }), HttpStatusCode.Conflict);
            await AssertProblemAsync(await client.DeleteAsync($"{linePath}?expectedVersion=2"), HttpStatusCode.Conflict);

            var stored = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(quote.Version, stored.Version);
            Assert.Equal(quote.TotalSalesAmount.Amount, stored.TotalSalesAmount);
            Assert.Equal(quote.Lines.Select(line => line.Id), stored.Lines.Select(line => line.LineId));
        }
        finally { await CleanupAsync(quote.Id); }
    }

    [PostgresApiFact]
    public async Task New_edit_routes_reject_invalid_inputs_and_missing_records_without_writing()
    {
        var quote = await SeedAsync();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var path = $"/quotes/{quote.Id}";
            var body = new { workItemCode = "PAINT", description = "涂装", unit = "m2", quantity = 10m, salesUnitPrice = 20m, expectedVersion = 3L };
            foreach (var invalid in new[]
            {
                body with { workItemCode = " " }, body with { description = " " }, body with { unit = " " },
                body with { quantity = 0m }, body with { quantity = -1m }, body with { salesUnitPrice = -1m },
                body with { expectedVersion = 0 }, body with { quantity = decimal.MaxValue }
            })
                await AssertProblemAsync(await client.PostAsJsonAsync($"{path}/lines", invalid), HttpStatusCode.BadRequest);

            await AssertProblemAsync(await client.PostAsJsonAsync("/quotes", new { projectId = Guid.Empty }), HttpStatusCode.BadRequest);
            var linePath = $"{path}/lines/{quote.Lines[0].Id}";
            await AssertProblemAsync(await client.PatchAsJsonAsync($"{linePath}/sales-unit-price",
                new { salesUnitPrice = -1m, expectedVersion = 3 }), HttpStatusCode.BadRequest);
            await AssertProblemAsync(await client.PatchAsJsonAsync($"{linePath}/sales-unit-price",
                new { expectedVersion = 3 }), HttpStatusCode.BadRequest);
            await AssertProblemAsync(await client.PostAsJsonAsync($"{path}/lines",
                new { workItemCode = "PAINT", description = "涂装", unit = "m2", quantity = 10m, expectedVersion = 3 }), HttpStatusCode.BadRequest);
            await AssertProblemAsync(await client.DeleteAsync(linePath), HttpStatusCode.BadRequest);
            await AssertProblemAsync(await client.DeleteAsync($"{linePath}?expectedVersion=0"), HttpStatusCode.BadRequest);
            var missingPath = $"{path}/lines/{Guid.NewGuid()}";
            await AssertProblemAsync(await client.PatchAsJsonAsync($"{missingPath}/sales-unit-price",
                new { salesUnitPrice = 22m, expectedVersion = 3 }), HttpStatusCode.NotFound);
            await AssertProblemAsync(await client.DeleteAsync($"{missingPath}?expectedVersion=3"), HttpStatusCode.NotFound);
            await AssertProblemAsync(await client.PostAsJsonAsync($"/quotes/{Guid.NewGuid()}/lines", body), HttpStatusCode.NotFound);
            await AssertProblemAsync(await client.PostAsJsonAsync($"/quotes/{Guid.Empty}/lines", body), HttpStatusCode.BadRequest);

            var stored = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(quote.Version, stored.Version);
            Assert.Equal(quote.TotalSalesAmount.Amount, stored.TotalSalesAmount);
            Assert.Equal(2, stored.Lines.Count);
        }
        finally { await CleanupAsync(quote.Id); }
    }

    [PostgresApiFact]
    public async Task Price_precision_zero_price_and_no_op_versions_are_preserved()
    {
        var quote = await SeedAsync();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var path = $"/quotes/{quote.Id}";
            var pricePath = $"{path}/lines/{quote.Lines[0].Id}/sales-unit-price";
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync(pricePath,
                new { salesUnitPrice = 20m, expectedVersion = 3 })).StatusCode);
            Assert.Equal(3L, (await client.GetFromJsonAsync<GetQuoteResult>(path))!.Version);
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync(pricePath,
                new { salesUnitPrice = 1.005m, expectedVersion = 3 })).StatusCode);
            var precision = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(1.005m, precision.Lines[0].SalesUnitPrice);
            Assert.Equal(100.50m, precision.Lines[0].LineAmount);
            Assert.Equal(150.48m, precision.TotalSalesAmount);
            Assert.Equal(4L, precision.Version);
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync(pricePath,
                new { salesUnitPrice = 0m, expectedVersion = 4 })).StatusCode);
            var zero = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(49.98m, zero.TotalSalesAmount);
            Assert.Equal(5L, zero.Version);
        }
        finally { await CleanupAsync(quote.Id); }
    }

    [PostgresApiFact]
    public async Task Concurrent_line_additions_have_one_winner_and_only_one_new_line()
    {
        var quote = await SeedAsync();
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var path = $"/quotes/{quote.Id}";
            var body = new { workItemCode = "NEW", description = "新增工程项", unit = "m2", quantity = 10m, salesUnitPrice = 20m, expectedVersion = 3 };
            var responses = await Task.WhenAll(client.PostAsJsonAsync($"{path}/lines", body), client.PostAsJsonAsync($"{path}/lines", body));
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            await AssertProblemAsync(Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict);
            var stored = (await client.GetFromJsonAsync<GetQuoteResult>(path))!;
            Assert.Equal(3, stored.Lines.Count);
            Assert.Equal(4L, stored.Version);
            Assert.Equal(2249.98m, stored.TotalSalesAmount);
        }
        finally { await CleanupAsync(quote.Id); }
    }

    private static async Task<AddQuoteLineResult> AddLineAsync(HttpClient client, Guid quoteId,
        string code, decimal quantity, decimal price, long version)
    {
        var response = await client.PostAsJsonAsync($"/quotes/{quoteId}/lines", new
        {
            workItemCode = code, description = "工程项", unit = "m2", quantity, salesUnitPrice = price, expectedVersion = version
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.EndsWith($"/quotes/{quoteId}", response.Headers.Location!.ToString());
        return (await response.Content.ReadFromJsonAsync<AddQuoteLineResult>())!;
    }

    private static async Task EnsureDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    private static async Task CleanupProjectAsync(Guid projectId)
    {
        await using var context = CreateContext();
        await context.Quotes.Where(quote => quote.ProjectId == projectId).ExecuteDeleteAsync();
    }
}
