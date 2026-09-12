namespace WebApp.Auth;

/// <summary>
/// The signed-in account where there has to be one. Every page under <c>[Authorize]</c> acts on
/// the account's data, and twenty call sites were unwrapping <c>UserId!.Value</c> to name it — a
/// null-forgiving operator that turns "not signed in" into a NullReferenceException with nothing
/// in it to say so. One spelling, and one sentence when it is wrong.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// The account's id, or an <see cref="InvalidOperationException"/> where there is no signed-in
    /// account — which behind <c>[Authorize]</c> is a broken cookie, not a state to handle. Code
    /// that can legitimately run anonymous keeps reading <see cref="ICurrentUserService.UserId"/>.
    /// </summary>
    public static Guid RequireUserId(this ICurrentUserService currentUser) =>
        currentUser.UserId ?? throw new InvalidOperationException(
            "No signed-in account — this runs only behind [Authorize].");
}
