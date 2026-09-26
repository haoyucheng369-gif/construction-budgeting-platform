namespace ConstructionBudgeting.Sales.Domain.Projects;

public sealed class Project
{
    public Guid Id { get; }
    public string Name { get; }

    public Project(Guid id, string name)
    {
        if (id == Guid.Empty) throw new ArgumentException("A project must have an ID.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A project must have a name.", nameof(name));

        name = name.Trim();
        if (name.Length > 200) throw new ArgumentException("A project name cannot exceed 200 characters.", nameof(name));

        Id = id;
        Name = name;
    }
}
