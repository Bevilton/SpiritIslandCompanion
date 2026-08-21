using System.Security.Claims;
using Infrastructure.Demo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApp.Demo;

/// <summary>
/// The public door into the demo sandbox. Mapped in every environment on purpose — the demo
/// is a landing-page feature, and the same mechanism is what lets a developer poke at a fully
/// populated account locally without mocking anything.
///
/// /demo — signs the visitor in as the demo account over a fresh private sandbox and drops
/// them on the dashboard. No account needed; anything they change stays in their copy.
///
/// /demo/reset — throws the visitor's sandbox away and builds a fresh one from the template.
/// The cookie (and so the sandbox id) stays the same.
///
/// /demo/exit — signs the demo cookie out and discards the sandbox.
/// </summary>
public static class DemoEndpoints
{
    public static WebApplication MapDemoEndpoints(this WebApplication app)
    {
        app.MapGet("/demo", async (HttpContext context, DemoSandboxRegistry registry) =>
        {
            // A navigation, or a request this site's own pages made — never a request some other
            // site issued. This endpoint hands out the same cookie a real account signs in with,
            // and on a cross-site request the guard below cannot see an existing session, because
            // SameSite=Lax withholds the cookie: a page elsewhere loading this URL as an image or
            // fetching it no-cors would look anonymous here and be handed a Set-Cookie that
            // replaces a signed-in visitor's own session with a demo one. Set-Cookie on a no-cors
            // response is honoured by the browser even though the script cannot read the body, so
            // the destination alone is not enough to tell the two apart.
            //
            // A cross-site *navigation* is the one thing that must keep working: a link to the
            // demo from anywhere on the web is the whole point of the feature. Everything else
            // has to come from this origin — which covers the fetch a Blazor enhanced navigation
            // issues (destination "empty", site "same-origin"). A browser old enough to send no
            // Sec-Fetch-Dest at all is taken at its word, as it was before.
            var destination = context.Request.Headers["Sec-Fetch-Dest"].ToString();
            var site = context.Request.Headers["Sec-Fetch-Site"].ToString();
            if (destination is not ("" or "document") && site is not ("same-origin" or "same-site" or "none"))
                return Results.NotFound();

            // A real account's session is never overwritten here. Signing in issues the same
            // cookie the demo does, so without this a signed-in visitor following the link would
            // be quietly signed out of their own account and into sample data. Leave the demo
            // first (or use a private window).
            if (context.User.Identity?.IsAuthenticated == true && !DemoClaims.IsDemo(context.User))
                return Results.Redirect("/app/dashboard");

            // Re-entering resets the island the visitor already has instead of minting a second
            // one: the cookie goes on naming the same sandbox, so a page they left open in
            // another tab keeps working rather than querying a database that is gone.
            if (DemoClaims.GetSandboxId(context.User) is { } existing)
            {
                await registry.ResetSandboxAsync(existing, context.RequestAborted);
                return Results.Redirect("/app/dashboard");
            }

            var sandboxId = await registry.CreateSandboxAsync(context.RequestAborted);

            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, DemoSandbox.DemoUserNickname),
                    new Claim(ClaimTypes.Email, DemoSandbox.DemoUserEmail),
                    new Claim("db_user_id", DemoSandbox.DemoUserId.ToString()),
                    new Claim(DemoClaims.SandboxId, sandboxId.ToString()),
                ],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.Redirect("/app/dashboard");
        }).AllowAnonymous();

        app.MapGet("/demo/reset", async (HttpContext context, DemoSandboxRegistry registry) =>
        {
            // Anonymous, not RequireAuthorization: an expired demo cookie or a stale link should
            // land on the landing page, and the default challenge scheme would send it to the
            // identity provider's login form instead.
            if (DemoClaims.GetSandboxId(context.User) is not { } sandboxId)
                return Results.Redirect("/");

            await registry.ResetSandboxAsync(sandboxId, context.RequestAborted);
            return Results.Redirect("/app/dashboard");
        }).AllowAnonymous();

        app.MapGet("/demo/exit", async (HttpContext context, DemoSandboxRegistry registry) =>
        {
            // Only a demo cookie is signed out — a real account reaching this URL keeps its session.
            if (DemoClaims.GetSandboxId(context.User) is not { } sandboxId)
                return Results.Redirect("/");

            registry.Remove(sandboxId);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        }).AllowAnonymous();

        return app;
    }
}
