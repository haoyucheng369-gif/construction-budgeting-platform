namespace ConstructionBudgeting.Sales.Application.Projects;

public sealed class ProjectNotFoundException(Guid projectId)
    : KeyNotFoundException($"Project '{projectId}' was not found.");
