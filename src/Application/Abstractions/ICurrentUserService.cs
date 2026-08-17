namespace Application.Abstractions;

/// <summary>
/// Provides access to the currently authenticated user's identity.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The external identity provider subject ID (e.g. Auth0 "sub" claim).
    /// Null if the user is not authenticated.
    /// </summary>
    string? ExternalId { get; }

    /// <summary>
    /// The local database user ID. Available after user sync.
    /// </summary>
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
