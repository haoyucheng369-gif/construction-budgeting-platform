using ConstructionBudgeting.Sales.Domain.Quotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

internal sealed class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.ToTable("QuoteLines", table =>
        {
            table.HasCheckConstraint("CK_QuoteLines_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_QuoteLines_Price", "\"SalesUnitPrice\" >= 0");
            table.HasCheckConstraint("CK_QuoteLines_Amount", "\"LineAmount\" >= 0 AND \"LineAmount\" = round(\"LineAmount\", 2)");
            table.HasCheckConstraint("CK_QuoteLines_Position", "\"Position\" >= 0");
        });
        builder.Property<Guid>("QuoteId");
        builder.HasKey("QuoteId", nameof(QuoteLine.Id));
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property<int>("Position");
        builder.HasIndex("QuoteId", "Position").IsUnique();
        builder.Property(line => line.WorkItemCode).IsRequired();
        builder.Property(line => line.Description).IsRequired();
        builder.Property(line => line.Unit).IsRequired();
        builder.Property(line => line.Quantity)
            .HasConversion(quantity => quantity.Value, value => new Quantity(value))
            .HasColumnType("numeric").IsRequired();
        builder.Property(line => line.SalesUnitPrice)
            .HasConversion(money => money.Amount, amount => new Money(amount, "EUR"))
            .HasColumnType("numeric").IsRequired();
        builder.Property(line => line.LineAmount)
            .HasConversion(money => money.Amount, amount => new Money(amount, "EUR"))
            .HasColumnType("numeric").IsRequired();
    }
}
