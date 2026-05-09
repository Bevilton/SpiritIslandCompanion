using Application.Features.Users;
using Domain.Models.Static;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApp.State;

/// <summary>
/// Per-circuit cache of the current user's owned expansions. Cascaded from
/// AppShell so that views and the game form can filter content to match the
/// user's collection. <see cref="Current"/> is null until loaded; null also
/// signals "no user / public context — show everything."
///
/// Resolves the user from <see cref="AuthenticationStateProvider"/> directly
/// (async) so we don't race the synchronous claim cache in CurrentUserService.
/// Loads via a dedicated DI scope so the EF query doesn't share the page's
/// scoped DbContext.
/// </summary>
public sealed class OwnedExpansionsState(
    IServiceScopeFactory scopeFactory,
    AuthenticationStateProvider authStateProvider)
{
    private bool _loaded;
    private Task? _inFlight;

    public IReadOnlyList<ExpansionId>? Current { get; private set; }
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
                Current = null;
            }
            else
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new GetUserQuery(userId.Value));
                if (result.IsSuccess)
                {
                    Current = result.Value.OwnedExpansionIds.Select(id => new ExpansionId(id)).ToList();
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

    public bool Includes(ExpansionId id)
        => Current is null || Current.Contains(id);
}
