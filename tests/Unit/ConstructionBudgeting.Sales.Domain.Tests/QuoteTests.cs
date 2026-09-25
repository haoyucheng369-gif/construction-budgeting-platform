using ConstructionBudgeting.Sales.Domain.Quotations;

namespace ConstructionBudgeting.Sales.Domain.Tests;

public sealed class QuoteTests
{
    [Fact]
    public void New_quote_belongs_to_a_project_and_starts_with_no_lines_or_sales()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var quote = new Quote(id, projectId);

        Assert.Equal(id, quote.Id);
        Assert.Equal(projectId, quote.ProjectId);
        Assert.Empty(quote.Lines);
        Assert.Equal(new Money(0m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Empty_quote_identity_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Quote(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Empty_project_identity_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Quote(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Adding_painting_and_flooring_lines_updates_the_quote_total()
    {
        var quote = CreateQuote();
        var paintingId = Guid.NewGuid();
        var flooringId = Guid.NewGuid();

        var painting = quote.AddLine(
            paintingId, "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));
        var flooring = quote.AddLine(
            flooringId, "FLOOR", "Floor installation", "m2", new Quantity(50m), new Money(40m));

        Assert.Collection(quote.Lines,
            line => Assert.Same(painting, line),
            line => Assert.Same(flooring, line));
        Assert.Equal(paintingId, painting.Id);
        Assert.Equal(flooringId, flooring.Id);
        Assert.Equal(new Money(4000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Same_work_item_can_appear_on_multiple_distinct_lines()
    {
        var quote = CreateQuote();

        AddPainting(quote);
        AddPainting(quote);

        Assert.Equal(2, quote.Lines.Count);
        Assert.NotEqual(quote.Lines[0].Id, quote.Lines[1].Id);
        Assert.Equal(new Money(4000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Duplicate_line_identity_is_rejected_without_changing_the_quote()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<InvalidOperationException>(() => quote.AddLine(
            original.Id, "FLOOR", "Floor installation", "m2", new Quantity(50m), new Money(40m)));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Total_sums_rounded_lines_rather_than_rounding_the_sum_of_raw_amounts()
    {
        var quote = CreateQuote();

        AddPainting(quote, quantity: 1m, price: 0.005m);
        AddPainting(quote, quantity: 1m, price: 0.005m);

        Assert.All(quote.Lines, line => Assert.Equal(new Money(0.01m), line.LineAmount));
        Assert.Equal(new Money(0.02m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Invalid_line_does_not_change_existing_lines_or_total()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<ArgumentOutOfRangeException>(() => AddPainting(quote, price: -1m));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Line_amount_overflow_does_not_change_the_quote()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);

        Assert.Throws<OverflowException>(() => AddPainting(quote, quantity: 2m, price: decimal.MaxValue));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Total_overflow_does_not_attach_the_new_line()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote, quantity: 1m, price: decimal.MaxValue);

        Assert.Throws<OverflowException>(() => AddPainting(quote, quantity: 1m, price: 1m));

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(decimal.MaxValue), quote.TotalSalesAmount);
    }

    [Fact]
    public void Exposed_collection_cannot_be_used_to_bypass_aggregate_operations()
    {
        var quote = CreateQuote();
        var original = AddPainting(quote);
        var collection = Assert.IsAssignableFrom<ICollection<QuoteLine>>(quote.Lines);

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(original));
        Assert.Throws<NotSupportedException>(() => collection.Clear());

        Assert.Same(original, Assert.Single(quote.Lines));
        Assert.Equal(new Money(2000m), quote.TotalSalesAmount);
    }

    [Fact]
    public void Quotes_create_separate_line_instances_and_keep_their_totals_independent()
    {
        var first = CreateQuote();
        var second = CreateQuote();
        var lineId = Guid.NewGuid();

        var firstLine = first.AddLine(
            lineId, "PAINT", "Wall painting", "m2", new Quantity(100m), new Money(20m));
        var secondLine = second.AddLine(
            lineId, "PAINT", "Wall painting", "m2", new Quantity(50m), new Money(20m));

        Assert.NotSame(firstLine, secondLine);
        Assert.Equal(new Money(2000m), first.TotalSalesAmount);
        Assert.Equal(new Money(1000m), second.TotalSalesAmount);
    }

    private static Quote CreateQuote() => new(Guid.NewGuid(), Guid.NewGuid());

    private static QuoteLine AddPainting(Quote quote, decimal quantity = 100m, decimal price = 20m)
    {
        return quote.AddLine(
            Guid.NewGuid(), "PAINT", "Wall painting", "m2", new Quantity(quantity), new Money(price));
    }
}
