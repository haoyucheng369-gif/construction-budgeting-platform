using ConstructionBudgeting.Sales.Application.Projects;
using MediatR;

namespace ConstructionBudgeting.Sales.Api;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var projects = app.MapGroup("/projects").WithTags("项目");

        projects.MapPost("", async (CreateProjectRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateProjectCommand(request.Name), cancellationToken);
            return TypedResults.CreatedAtRoute(value: result, routeName: "GetProject", routeValues: new { id = result.ProjectId });
        })
            .WithName("CreateProject")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("创建项目")
            .WithDescription("只记录项目名称；创建后将返回的 projectId 用于创建报价。");

        projects.MapGet("", async (ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new ListProjectsQuery(), cancellationToken)))
            .WithName("ListProjects")
            .WithSummary("列出项目")
            .WithDescription("按名称、ID 排序，供报价创建时选择项目。");

        projects.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            TypedResults.Ok(await sender.Send(new GetProjectQuery(id), cancellationToken)))
            .WithName("GetProject")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("查询项目");
    }
}
