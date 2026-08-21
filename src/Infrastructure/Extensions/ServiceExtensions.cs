using Application.Abstractions;
using Application.Data;
using Infrastructure.Database;
using Infrastructure.Demo;
using Infrastructure.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ProcessEventsInterceptor>();

        services.AddSingleton<DemoSandboxRegistry>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

        // Every reader and writer in the application layer goes through IAppDbContext, so
        // this one registration is the demo sandbox's entire isolation boundary: a scope that
        // belongs to a demo session gets that visitor's throwaway in-memory copy, and every
        // other scope gets the real database exactly as before. The web host says which is
        // which through IDemoSessionLocator (the sandbox id rides in the demo auth cookie).
        services.AddScoped<IAppDbContext>(sp =>
        {
            var sandboxId = sp.GetService<IDemoSessionLocator>()?.SandboxId;
            if (sandboxId is null)
                return sp.GetRequiredService<AppDbContext>();

            return sp.GetRequiredService<DemoSandboxRegistry>().CreateContext(sandboxId.Value);
        });
    }
}
