using ConstructionBudgeting.Sales.Domain.Quotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConstructionBudgeting.Sales.Infrastructure.Persistence;

internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes", table =>
        {
            table.HasCheckConstraint("CK_Quotes_Version", "\"Version\" >= 1");
            table.HasCheckConstraint("CK_Quotes_Currency", "\"Currency\" = 'EUR'");
            table.HasCheckConstraint("CK_Quotes_Total", "\"TotalSalesAmount\" >= 0 AND \"TotalSalesAmount\" = round(\"TotalSalesAmount\", 2)");
        });
        builder.HasKey(quote => quote.Id);
        builder.Property(quote => quote.Id).ValueGeneratedNever();
        builder.Property(quote => quote.ProjectId).IsRequired();
        builder.HasIndex(quote => quote.ProjectId).IsUnique();
        builder.Property(quote => quote.Version).IsConcurrencyToken().ValueGeneratedNever();
        builder.Property(quote => quote.TotalSalesAmount)
            .HasConversion(money => money.Amount, amount => new Money(amount, "EUR"))
            .HasColumnType("numeric").IsRequired();
        // 按当前领域约定，所有 Money 都使用欧元，无需建立独立的币种实体。
        builder.Property<string>("Currency").HasMaxLength(3).HasDefaultValue("EUR").IsRequired();
        builder.HasMany(quote => quote.Lines).WithOne().HasForeignKey("QuoteId")
            .IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(quote => quote.Lines).HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
