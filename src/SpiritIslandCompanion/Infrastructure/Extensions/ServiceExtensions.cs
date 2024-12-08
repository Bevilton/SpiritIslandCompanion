using Domain.Data;
using Infrastructure.Database;
using Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(
            (sp, options) =>
            {
                var interceptor = sp.GetRequiredService<ProcessEventsInterceptor>();

                options
                    .UseSqlServer(configuration.GetConnectionString("SIDatabase"),
                        x => x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                    .AddInterceptors(interceptor);
            });

        services.AddScoped<ProcessEventsInterceptor>();
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    }
}