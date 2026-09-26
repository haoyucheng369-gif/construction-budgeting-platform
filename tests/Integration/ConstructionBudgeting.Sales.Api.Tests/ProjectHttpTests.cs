using System.Net;
using System.Net.Http.Json;
using ConstructionBudgeting.Sales.Application.Projects;
using ConstructionBudgeting.Sales.Application.Quotations.CreateQuote;
using Microsoft.EntityFrameworkCore;

namespace ConstructionBudgeting.Sales.Api.Tests;

public sealed partial class QuoteHttpTests
{
    [PostgresApiFact]
    public async Task Project_can_be_created_selected_and_used_by_a_quote()
    {
        await EnsureDatabaseAsync();
        var projectId = Guid.Empty;
        try
        {
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();

            var createdResponse = await client.PostAsJsonAsync("/projects", new { name = "  巴黎办公楼翻新  " });
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = (await createdResponse.Content.ReadFromJsonAsync<ProjectDetails>())!;
            projectId = created.ProjectId;
            Assert.NotEqual(Guid.Empty, projectId);
            Assert.Equal("巴黎办公楼翻新", created.Name);
            Assert.EndsWith($"/projects/{projectId}", createdResponse.Headers.Location!.ToString());

            Assert.Equal(created, await client.GetFromJsonAsync<ProjectDetails>($"/projects/{projectId}"));
            var projects = (await client.GetFromJsonAsync<ProjectDetails[]>("/projects"))!;
            Assert.Contains(created, projects);

            var quoteResponse = await client.PostAsJsonAsync("/quotes", new { projectId });
            Assert.Equal(HttpStatusCode.Created, quoteResponse.StatusCode);
            Assert.Equal(projectId, (await quoteResponse.Content.ReadFromJsonAsync<CreateQuoteResult>())!.ProjectId);

            await AssertProblemAsync(await client.PostAsJsonAsync("/quotes", new { projectId = Guid.NewGuid() }),
                HttpStatusCode.NotFound);
            await AssertProblemAsync(await client.GetAsync($"/projects/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
            await AssertProblemAsync(await client.PostAsJsonAsync("/projects", new { name = "  " }),
                HttpStatusCode.BadRequest);
            await AssertProblemAsync(await client.PostAsJsonAsync("/projects", new { name = new string('x', 201) }),
                HttpStatusCode.BadRequest);

            await using var context = CreateContext();
            Assert.Single(await context.Quotes.Where(quote => quote.ProjectId == projectId).ToListAsync());
        }
        finally
        {
            if (projectId != Guid.Empty) await CleanupProjectAsync(projectId);
        }
    }
}
