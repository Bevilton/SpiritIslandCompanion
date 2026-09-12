namespace WebApp.Auth;

/// <summary>
/// Who the current circuit is signed in as, resolved from the auth cookie's
/// <see cref="AppClaims.UserId"/> claim. Lives with the rest of the web host's identity code:
/// every consumer is a component, and nothing in the application layer ever read it — the
/// account id reaches the handlers as a command or query argument, which is the seam that keeps
/// them testable without a signed-in user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The local database user ID. Available after user sync;
    /// null if the user is not authenticated.
    /// </summary>
    Guid? UserId { get; }
}
