using System.Security.Claims;
using Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace WebApp.Auth;

/// <summary>
/// OIDC event handler that syncs the authenticated user to the local database on each sign-in.
/// Matches users by email. After sync, the local database UserId is added as a claim.
/// </summary>
public sealed class UserSyncOidcEvents(IServiceProvider serviceProvider) : OpenIdConnectEvents
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
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new SyncUserCommand(email, name ?? email));

        if (result.IsSuccess)
        {
            identity.AddClaim(new Claim("db_user_id", result.Value.UserId.ToString()));
        }
    }
}
