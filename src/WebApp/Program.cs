using ApexCharts;
using Application.Extensions;
using Infrastructure.Database;
using Infrastructure.Demo;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using WebApp;
using WebApp.Auth;
using WebApp.Components;
using WebApp.Demo;
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

// Who the circuit is: the account behind the auth cookie, and the account's own context.
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
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
    app.UseExceptionHandler(AppRoutes.Error, createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// After authentication and authorization, as the antiforgery middleware requires: its tokens are
// bound to the signed-in principal, which does not exist yet a step earlier in the pipeline.
app.UseAntiforgery();

app.MapAuthEndpoints();
app.MapDemoEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
