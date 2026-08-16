using Application.Features.Users;
using Domain.Models.Static;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApp.State;

/// <summary>
/// Per-circuit cache of the signed-in user's app context. Loaded once when first
/// observed and kept in sync via the <see cref="Changed"/> event. AppShell is the
/// canonical consumer — it subscribes here and cascades individual properties
/// (e.g. <see cref="OwnedExpansions"/>) down the component tree, so pages and
/// shared components can stay auth-agnostic and just declare a
/// <see cref="Microsoft.AspNetCore.Components.CascadingParameterAttribute"/>.
///
/// Use this for things that are user-scoped, read-heavy, change-rare (owned
/// expansions, preferences, theme). Things that change often or come from
/// other users (friends list, games list) should be queried per page instead.
///
/// Resolves the user from <see cref="AuthenticationStateProvider"/> directly
/// (async) so we don't race the synchronous claim cache in CurrentUserService.
/// Loads via a dedicated DI scope so the EF query doesn't share the page's
/// scoped DbContext.
/// </summary>
public sealed class CurrentUserState(
    IServiceScopeFactory scopeFactory,
    AuthenticationStateProvider authStateProvider)
{
    private bool _loaded;
    private Task? _inFlight;

    /// <summary>
    /// The user's owned expansions. Null while loading or for anonymous visitors —
    /// consumers should treat null as "no filtering, show everything."
    /// </summary>
    public IReadOnlyList<ExpansionId>? OwnedExpansions { get; private set; }

    /// <summary>The signed-in account, or null while loading / for anonymous visitors.</summary>
    public GetUserResponse? User { get; private set; }

    /// <summary>What to print for the signed-in user: their nickname, or their email until set.</summary>
    public string? DisplayName => User?.DisplayName;

    /// <summary>
    /// True once we know the account exists and has never been named. AppShell reads this
    /// to raise the first-login prompt; it stays false while loading, so the prompt never
    /// flashes up on a user who does have a name.
    /// </summary>
    public bool NeedsNickname => _loaded && User is { Nickname: null };

    public event Action? Changed;

    public Task EnsureLoadedAsync()
        => _loaded ? Task.CompletedTask : ReloadAsync();

    public Task ReloadAsync()
        => _inFlight ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var userId = await GetUserIdAsync();
            if (userId is null)
            {
                OwnedExpansions = null;
                User = null;
            }
            else
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new GetUserQuery(userId.Value));
                if (result.IsSuccess)
                {
                    User = result.Value;
                    OwnedExpansions = result.Value.OwnedExpansionIds.Select(id => new ExpansionId(id)).ToList();
                }
            }

            _loaded = true;
            Changed?.Invoke();
        }
        finally
        {
            _inFlight = null;
        }
    }

    private async Task<Guid?> GetUserIdAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        var claim = state.User.FindFirst("db_user_id")?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}
