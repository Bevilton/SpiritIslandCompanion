namespace Infrastructure.Demo;

/// <summary>
/// The identities the demo sandbox is built around. Fixed rather than minted at seeding
/// time: the demo visitor's auth cookie carries the demo account's user id, and the cookie
/// outlives both the sandbox (idle-evicted) and the process (template rebuilt on restart) —
/// so every rebuild has to produce the account the cookie already names.
/// </summary>
public static class DemoSandbox
{
    /// <summary>The account every demo visitor is signed in as, inside their own sandbox.</summary>
    public static readonly Guid DemoUserId = Guid.Parse("d3300000-0000-4000-8000-000000000001");

    public const string DemoUserEmail = "demo@spirit-island-companion.local";
    public const string DemoUserNickname = "Demo Islander";
}
