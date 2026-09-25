using System.Text.Json.Serialization;

namespace ConstructionBudgeting.Sales.Api;

public sealed record ChangeSalesUnitPriceRequest([property: JsonRequired] decimal SalesUnitPrice, long ExpectedVersion);
