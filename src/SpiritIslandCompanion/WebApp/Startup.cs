using Application.Extensions;
using Infrastructure.Extensions;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using WebApp.Components;

namespace WebApp;

public class Startup
{
    private readonly WebApplicationBuilder _builder;

    public Startup(WebApplicationBuilder builder)
    {
        _builder = builder;
    }

    public void ConfigureServices()
    {
        AddObservability(_builder.Services, _builder.Logging, _builder.Configuration, x =>
        {
            x.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();
        }, x =>
        {
            if (_builder.Environment.IsDevelopment())
            {
                x.SetSampler<AlwaysOnSampler>();
            }

            x.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation();
        });


        _builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        _builder.Services.AddApplication();
        _builder.Services.AddInfrastructure(_builder.Configuration);
    }

    public void Configure(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSerilogRequestLogging();

        app.UseRouting();
        //app.UseAuthentication();
        //app.UseAuthorization();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }

    private static void AddObservability(IServiceCollection services, ILoggingBuilder builder,
        IConfiguration configuration,
        Action<MeterProviderBuilder> meterBuilderAction, Action<TracerProviderBuilder> tracerBuilderAction)
    {
        builder.AddOpenTelemetry(x =>
        {
            x.IncludeScopes = true;
            x.IncludeFormattedMessage = true;
            x.AddOtlpExporter();
        });

        var serviceName = "SpiritislandCompanion";
        var serviceId = "si-companion";

        services.AddSerilog((sp, config) =>
        {
            config.ReadFrom.Configuration(configuration);
            config.Enrich.WithProperty("Application", serviceName);
            config.WriteTo.OpenTelemetry(x =>
            {
                x.ResourceAttributes = new Dictionary<string, object>
                {
                    { "service.name", serviceName },
                    { "service.instance.id", serviceId },
                };
            });
            config.WriteTo.Console();
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(x =>
                x.AddService(serviceName, serviceInstanceId: serviceId))
            .WithMetrics(x =>
            {
                meterBuilderAction(x);

                x.AddOtlpExporter();
            })
            .WithTracing(x =>
            {
                tracerBuilderAction(x);

                x.AddOtlpExporter();
            });
    }
}