using System.Text.Json.Serialization;

namespace ConstructionBudgeting.Sales.Api;

public sealed record AddQuoteLineRequest(string WorkItemCode, string Description, string Unit,
    decimal Quantity, [property: JsonRequired] decimal SalesUnitPrice, long ExpectedVersion);
