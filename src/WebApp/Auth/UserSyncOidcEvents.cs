using System.Security.Claims;
using Application.Abstractions;
using Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace WebApp.Auth;

/// <summary>
/// OIDC event handler that syncs the authenticated user to the local database on each sign-in.
/// Matches users by email. After sync, the local database UserId is added as a claim.
/// </summary>
public sealed class UserSyncOidcEvents(IServiceProvider serviceProvider, IConfiguration configuration) : OpenIdConnectEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity is not ClaimsIdentity identity)
            return;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("email")?.Value;
        var name = principal.FindFirst("nickname")?.Value
                   ?? principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? principal.FindFirst("name")?.Value
                   ?? email;

        if (string.IsNullOrEmpty(email))
            return;

        using var scope = serviceProvider.CreateScope();
        // Accounts live in the real database, unconditionally. A visitor signing in from the
        // demo still carries the demo cookie on this callback request, and without the pin
        // the sync would create their account inside that throwaway sandbox.
        scope.ServiceProvider.GetRequiredService<IDemoSessionLocator>().PinToRealDatabase();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new SyncUserCommand(email));

        if (result.IsSuccess)
        {
            identity.AddClaim(new Claim("db_user_id", result.Value.UserId.ToString()));
            // Only ever a suggestion for the name prompt — the account's own nickname is set by
            // the user and lives in our database, never overwritten from the provider's claims.
            if (!string.IsNullOrWhiteSpace(name))
                identity.AddClaim(new Claim(SuggestedNameClaim, name));
        }
    }

    /// <summary>Claim carrying the identity provider's guess at the user's name, used to
    /// pre-fill the first-login name prompt.</summary>
    public const string SuggestedNameClaim = "suggested_name";

    public override Task RedirectToIdentityProviderForSignOut(RedirectContext context)
    {
        var oidc = configuration.GetSection("Oidc");
        var authority = oidc["Authority"]?.TrimEnd('/');
        var clientId = oidc["ClientId"];

        var logoutUri = $"{authority}/v2/logout?client_id={clientId}";

        var postLogoutUri = context.Properties.RedirectUri;
        if (!string.IsNullOrEmpty(postLogoutUri))
        {
            if (postLogoutUri.StartsWith('/'))
            {
                var request = context.Request;
                postLogoutUri = $"{request.Scheme}://{request.Host}{request.PathBase}{postLogoutUri}";
            }

            logoutUri += $"&returnTo={Uri.EscapeDataString(postLogoutUri)}";
        }

        context.Response.Redirect(logoutUri);
        context.HandleResponse();
        return Task.CompletedTask;
    }
}
