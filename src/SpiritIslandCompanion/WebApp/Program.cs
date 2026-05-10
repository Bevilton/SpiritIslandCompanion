using Application.Extensions;
using Infrastructure.Database;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using WebApp.Auth;
using WebApp.Components;
using WebApp.State;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<AppDbContext>("SIDatabase");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddOidcAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CurrentUserState>();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
