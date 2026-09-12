using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApp.Auth;

/// <summary>
/// Provides the current user's identity from the Blazor authentication state.
/// The local UserId is resolved from the <see cref="AppClaims.UserId"/> claim set during
/// OIDC sign-in.
/// </summary>
public sealed class CurrentUserService(AuthenticationStateProvider authStateProvider) : ICurrentUserService
{
    private ClaimsPrincipal? _user;
    private bool _resolved;

    public Guid? UserId => AppClaims.GetUserId(GetUser());

    /// <summary>
    /// Synchronous access works in Blazor Server because the provider caches the state after
    /// the first async resolution in the circuit. The answer is only remembered once there
    /// <em>is</em> one: a read that lands before the state has resolved must ask again next
    /// time, or the whole circuit would be treated as anonymous for its lifetime — and every
    /// <see cref="CurrentUserExtensions.RequireUserId"/> behind <c>[Authorize]</c> would throw.
    /// </summary>
    private ClaimsPrincipal? GetUser()
    {
        if (_resolved) return _user;

        try
        {
            var task = authStateProvider.GetAuthenticationStateAsync();
            if (!task.IsCompletedSuccessfully) return null;
            _user = task.Result.User;
            _resolved = true;
        }
        catch
        {
            // Not a circuit scope — the provider refuses outside one, and there is no principal
            // here to read. That answer is final for the scope.
            _resolved = true;
        }

        return _user;
    }
}
