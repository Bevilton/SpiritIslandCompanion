using System.Security.Claims;
using Application.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApp.Auth;

/// <summary>
/// Provides the current user's identity from the Blazor authentication state.
/// The local UserId is resolved from the "db_user_id" claim set during OIDC sign-in.
/// </summary>
public sealed class CurrentUserService(AuthenticationStateProvider authStateProvider) : ICurrentUserService
{
    private ClaimsPrincipal? _user;
    private bool _initialized;

    public string? ExternalId => GetUser()?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public Guid? UserId
    {
        get
        {
            var claim = GetUser()?.FindFirst("db_user_id")?.Value;
            return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => GetUser()?.Identity?.IsAuthenticated ?? false;

    private ClaimsPrincipal? GetUser()
    {
        if (!_initialized)
        {
            // Synchronous access — works in Blazor Server since AuthenticationStateProvider
            // caches the state after the first async resolution in the circuit.
            try
            {
                var task = authStateProvider.GetAuthenticationStateAsync();
                if (task.IsCompletedSuccessfully)
                    _user = task.Result.User;
            }
            catch
            {
                // Swallow — user remains null
            }

            _initialized = true;
        }

        return _user;
    }
}
