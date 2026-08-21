using Application.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApp.Demo;

/// <summary>
/// Resolves the current scope's demo sandbox from the signed-in principal, the same two ways
/// the rest of the app reads identity: the Blazor authentication state inside a circuit, and
/// the HttpContext for endpoint requests. Manually created scopes have neither — whoever
/// creates one on a demo visitor's behalf pins it explicitly (see CurrentUserState).
/// </summary>
public sealed class DemoSessionLocator(
    AuthenticationStateProvider authStateProvider,
    IHttpContextAccessor httpContextAccessor) : IDemoSessionLocator
{
    private bool _pinned;
    private Guid? _pin;
    private bool _resolved;
    private Guid? _value;

    public void PinToSandbox(Guid sandboxId)
    {
        _pin = sandboxId;
        _pinned = true;
    }

    public void PinToRealDatabase()
    {
        _pin = null;
        _pinned = true;
    }

    public Guid? SandboxId
    {
        get
        {
            if (_pinned)
                return _pin;

            if (!_resolved)
            {
                _value = Resolve();
                _resolved = true;
            }

            return _value;
        }
    }

    private Guid? Resolve()
    {
        // Synchronous access works in Blazor Server because the provider caches the state
        // after its first async resolution in the circuit — same pattern as CurrentUserService.
        try
        {
            var task = authStateProvider.GetAuthenticationStateAsync();
            if (task.IsCompletedSuccessfully)
            {
                // The circuit's principal is authoritative — including when it carries no
                // sandbox claim. Falling through to the HttpContext here would let another
                // (possibly stale) principal answer for a signed-in circuit.
                return DemoClaims.GetSandboxId(task.Result.User);
            }
        }
        catch
        {
            // Not a circuit scope — the request principal is the one to read.
        }

        return DemoClaims.GetSandboxId(httpContextAccessor.HttpContext?.User);
    }
}
