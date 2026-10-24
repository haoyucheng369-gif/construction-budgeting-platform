using ConstructionBudgeting.Sales.Application.Projects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionBudgeting.Sales.Api.Controllers;

[ApiController]
[Route("projects")]
[Tags("项目")]
public sealed class ProjectsController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "CreateProject")]
    [EndpointSummary("创建项目")]
    [EndpointDescription("只记录项目名称；创建后返回的 projectId 用于创建报价。")]
    [ProducesResponseType(typeof(ProjectDetails), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateProjectCommand(request.Name), cancellationToken);
        return CreatedAtRoute("GetProject", new { id = result.ProjectId }, result);
    }

    [HttpGet(Name = "ListProjects")]
    [EndpointSummary("列出项目")]
    [EndpointDescription("按名称、ID 排序，供报价创建时选择项目。")]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectDetails>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectDetails>>> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListProjectsQuery(), cancellationToken));

    [HttpGet("{id:guid}", Name = "GetProject")]
    [EndpointSummary("查询项目")]
    [ProducesResponseType(typeof(ProjectDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ProjectDetails>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProjectQuery(id), cancellationToken));
}
