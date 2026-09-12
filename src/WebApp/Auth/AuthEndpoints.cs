using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace WebApp.Auth;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet(AppRoutes.Login, async (HttpContext context, string? returnUrl) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = LocalOrHome(returnUrl)
            };
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
        }).AllowAnonymous();

        app.MapGet(AppRoutes.Logout, async (HttpContext context) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = AppRoutes.Home
            };
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
        }).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// The address to land on after signing in — only ever one of this site's own. The value
    /// comes straight off the query string of a public endpoint, and the OIDC handler redirects
    /// to it without a check of its own, so an absolute or protocol-relative address here would
    /// have turned <c>/login</c> into an open redirect: sign in at this site, land on another.
    /// <see cref="AppRoutes.LoginReturningTo"/> only ever supplies a local path, which is what
    /// this insists on.
    /// </summary>
    private static string LocalOrHome(string? returnUrl) =>
        returnUrl is ['/', not ('/' or '\\'), ..] ? returnUrl : AppRoutes.Home;
}
