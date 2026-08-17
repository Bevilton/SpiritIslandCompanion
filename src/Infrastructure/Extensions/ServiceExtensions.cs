using Application.Data;
using Infrastructure.Database;
using Infrastructure.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ProcessEventsInterceptor>();
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    }
}
