using System.Security.Claims;

namespace WebApp.Demo;

/// <summary>How a demo session is recognised: by the sandbox id its auth cookie carries.</summary>
public static class DemoClaims
{
    public const string SandboxId = "demo_sandbox_id";

    public static bool IsDemo(ClaimsPrincipal? principal) => GetSandboxId(principal) is not null;

    public static Guid? GetSandboxId(ClaimsPrincipal? principal)
    {
        var claim = principal?.FindFirst(SandboxId)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}
