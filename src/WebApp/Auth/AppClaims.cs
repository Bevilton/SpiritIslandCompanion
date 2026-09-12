using System.Security.Claims;

namespace WebApp.Auth;

/// <summary>
/// The claims this application adds to a signed-in principal of its own, and how they are read
/// back. The identity provider knows the visitor by email; the app knows them by the row in its
/// own database, and <see cref="UserId"/> is the bridge — written once at sign-in
/// (<see cref="UserSyncOidcEvents"/>, and by the demo endpoint for its sandbox account) and read
/// on every request that acts on the account's data.
/// <para>
/// The name was a bare literal in four files and the six lines that read it were written twice,
/// once in <c>CurrentUserService</c> and once in <c>CurrentUserState</c> — so a typo in either
/// spelling would have come back as "not signed in" rather than as a compile error.
/// <see cref="WebApp.Demo.DemoClaims"/> is the same shape for the claim that marks a demo session.
/// </para>
/// </summary>
public static class AppClaims
{
    /// <summary>The account's primary key in this application's own database.</summary>
    public const string UserId = "db_user_id";

    /// <summary>
    /// The identity provider's guess at the visitor's name, kept only to pre-fill the first-login
    /// prompt. Never the account's name: that is the nickname the user sets, it lives in our own
    /// database, and it is never overwritten from the provider's claims.
    /// </summary>
    public const string SuggestedName = "suggested_name";

    /// <summary>
    /// The signed-in account, or null for an anonymous visitor — and for a principal carrying a
    /// claim that is not a Guid, which is a malformed cookie rather than a session to trust.
    /// </summary>
    public static Guid? GetUserId(ClaimsPrincipal? principal)
    {
        var claim = principal?.FindFirst(UserId)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary><inheritdoc cref="SuggestedName"/></summary>
    public static string? GetSuggestedName(ClaimsPrincipal? principal) =>
        principal?.FindFirst(SuggestedName)?.Value;
}
