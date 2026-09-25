using ConstructionBudgeting.Sales.Application.Quotations.ChangeQuoteLineQuantity;
using Microsoft.Extensions.DependencyInjection;

namespace ConstructionBudgeting.Sales.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<ChangeQuoteLineQuantityHandler>());

        return services;
    }
}
