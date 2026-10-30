using Microsoft.Extensions.DependencyInjection;
using BonusTest.Application.Catalogs;
using BonusTest.Application.Bonuses;

namespace BonusTest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CatalogService>();

        services.AddScoped<BonusService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}

