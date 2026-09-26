using ConstructionBudgeting.Sales.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", table =>
            table.HasCheckConstraint("CK_Projects_Name", "length(btrim(\"Name\")) > 0"));
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Id).ValueGeneratedNever();
        builder.Property(project => project.Name).HasMaxLength(200).IsRequired();
    }
}
