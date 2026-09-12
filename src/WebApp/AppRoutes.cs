namespace WebApp;

/// <summary>
/// Every address the app links to that is not built with a query, on both sides of the sign-in.
/// The three that <em>are</em> built with one keep their own homes and are not repeated here —
/// <see cref="Components.Shared.Games.Filtering.GamesLink"/>,
/// <see cref="Components.Shared.Stats.StatsLink"/> and
/// <see cref="Components.Shared.People.PeopleLink"/> each own their path alongside the builders
/// that need it, so every route in the app has exactly one spelling.
/// <para>
/// Named because the dashboard alone was written out eight times — the sidebar, both shells'
/// logos, the landing page's call to action, the dashboard's own tile row and four demo
/// redirects — while its three neighbours in the same sidebar list had constants. A route typed
/// by hand at eight call sites is eight places to miss when one moves. The public side had the
/// same shape: <c>/login</c> at seven sites, the four reference pages at ten, the demo's three
/// doors at six — and the endpoints behind them mapped by a literal of their own, so a link and
/// its handler could be spelled apart.
/// </para>
/// <para>
/// The <c>@@page</c> directives stay literal: Blazor's router reads them at compile time and a
/// constant cannot stand in for one. These are for the links and the endpoint maps.
/// </para>
/// </summary>
public static class AppRoutes
{
    /// <summary>The landing page — and where a sign-out, a demo exit and a way home all land.</summary>
    public const string Home = "/";

    /// <summary>
    /// The OIDC challenge and the sign-out (see <c>AuthEndpoints</c>). Endpoints, not Blazor
    /// routes: following either hands the request to the identity provider and leaves the circuit.
    /// </summary>
    public const string Login = "/login";
    public const string Logout = "/logout";

    /// <summary><see cref="Login"/> with the address to come back to, for an anonymous visitor
    /// bounced off an authorised page — so signing in lands them where they were going.</summary>
    public static string LoginReturningTo(string returnUrl) =>
        $"{Login}?returnUrl={Uri.EscapeDataString(returnUrl)}";

    /// <summary>The demo sandbox's three doors — in, start over, out. See <c>DemoEndpoints</c>.</summary>
    public const string Demo = "/demo";
    public const string DemoReset = "/demo/reset";
    public const string DemoExit = "/demo/exit";

    /// <summary>Where an unhandled error lands — the exception handler's target, and the one
    /// page rendered statically (see <c>Error.razor</c>).</summary>
    public const string Error = "/Error";

    public const string Dashboard = "/app/dashboard";
    public const string Settings = "/app/settings";

    /// <summary>The signed-in catalogue routes — the public ones below drop the <c>/app</c>.</summary>
    public const string Spirits = "/app/spirits";
    public const string Adversaries = "/app/adversaries";
    public const string Scenarios = "/app/scenarios";
    public const string Boards = "/app/boards";

    /// <summary>The same catalogues for anyone, signed in or not — the public reference pages.</summary>
    public const string PublicSpirits = "/spirits";
    public const string PublicAdversaries = "/adversaries";
    public const string PublicScenarios = "/scenarios";
    public const string PublicBoards = "/boards";
}
