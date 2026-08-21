using ApexCharts;
using Application.Abstractions;
using Application.Extensions;
using Infrastructure.Database;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using WebApp.Auth;
using WebApp.Components;
using WebApp.Demo;
using WebApp.Dev;
using WebApp.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<AppDbContext>("SIDatabase");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddApexCharts();
builder.Services.AddOidcAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CurrentUserState>();

// The demo sandbox: how a scope learns it belongs to a demo session (the locator reads the
// sandbox claim), and the background build of the template every sandbox is copied from.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDemoSessionLocator, DemoSessionLocator>();
builder.Services.AddHostedService<DemoTemplateWarmup>();

var app = builder.Build();

// Apply any pending EF Core migrations on startup. Creates the database if it
// doesn't exist yet and brings the schema up to date with the latest migration.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapDemoEndpoints();
app.MapDevEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
