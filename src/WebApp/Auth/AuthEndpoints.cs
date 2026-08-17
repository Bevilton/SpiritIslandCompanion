using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace WebApp.Auth;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/login", async (HttpContext context, string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/"
            };
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
        }).AllowAnonymous();

        app.MapGet("/logout", async (HttpContext context) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/"
            };
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
        }).RequireAuthorization();

        return app;
    }
}
