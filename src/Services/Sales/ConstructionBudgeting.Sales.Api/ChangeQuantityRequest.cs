namespace ConstructionBudgeting.Sales.Api;

public sealed record ChangeQuantityRequest(decimal Quantity, long ExpectedVersion);
