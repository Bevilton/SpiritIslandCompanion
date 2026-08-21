namespace Application.Abstractions;

/// <summary>
/// Which demo sandbox the current scope belongs to, if any. Follows the same layering as
/// <see cref="ICurrentUserService"/>: the web host implements it (the sandbox id travels in
/// the demo visitor's auth cookie), and the infrastructure reads it to decide whether the
/// scope's <see cref="Application.Data.IAppDbContext"/> is the real database or that
/// visitor's throwaway copy.
/// </summary>
public interface IDemoSessionLocator
{
    /// <summary>The sandbox this scope works against, or null for the real database.</summary>
    Guid? SandboxId { get; }

    /// <summary>
    /// Binds this scope to a specific sandbox regardless of what any principal says. For the
    /// scopes that have no usable principal to read: manually created service scopes doing
    /// work for a demo visitor, and the seeding scope that fills the template itself.
    /// </summary>
    void PinToSandbox(Guid sandboxId);

    /// <summary>
    /// Binds this scope to the real database regardless of what any principal says. For the
    /// sign-in flows: a visitor arriving from the demo still carries the demo cookie while
    /// they authenticate, and a user sync that trusted it would create their real account
    /// inside a throwaway sandbox.
    /// </summary>
    void PinToRealDatabase();
}
