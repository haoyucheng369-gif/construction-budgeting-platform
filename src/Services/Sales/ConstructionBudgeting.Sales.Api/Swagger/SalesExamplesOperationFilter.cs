using ConstructionBudgeting.Sales.Infrastructure.Persistence;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ConstructionBudgeting.Sales.Api.Swagger;

public sealed class SalesExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 这些值只预填 Swagger 表单，不是服务器的默认业务参数。
        foreach (var parameter in operation.Parameters)
        {
            parameter.Schema.Default = parameter.Name switch
            {
                "id" or "quoteId" => new OpenApiString(SalesSampleData.QuoteId.ToString()),
                "lineId" => new OpenApiString(operation.OperationId == "RemoveQuoteLine"
                    ? "44444444-4444-4444-4444-444444444444" : "33333333-3333-3333-3333-333333333333"),
                "expectedVersion" => new OpenApiLong(3),
                _ => parameter.Schema.Default
            };
        }

        OpenApiObject? example = operation.OperationId switch
        {
            "CreateQuote" => new() { ["projectId"] = new OpenApiString("55555555-5555-5555-5555-555555555555") },
            "AddQuoteLine" => new()
            {
                ["workItemCode"] = new OpenApiString("PAINT"),
                ["description"] = new OpenApiString("墙面涂装"),
                ["unit"] = new OpenApiString("m2"),
                ["quantity"] = new OpenApiDouble(10),
                ["salesUnitPrice"] = new OpenApiDouble(20),
                ["expectedVersion"] = new OpenApiLong(3)
            },
            "ChangeQuoteLineQuantity" => new()
            {
                ["quantity"] = new OpenApiDouble(120), ["expectedVersion"] = new OpenApiLong(3)
            },
            "ChangeQuoteLineSalesUnitPrice" => new()
            {
                ["salesUnitPrice"] = new OpenApiDouble(22), ["expectedVersion"] = new OpenApiLong(3)
            },
            _ => null
        };
        if (example is not null && operation.RequestBody is not null)
        {
            foreach (var content in operation.RequestBody.Content.Values) content.Example = example;
        }
    }
}
