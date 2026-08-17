namespace WebApp.Components.Shared.Stats;

/// <summary>
/// Shared colour palette for ApexCharts. Keeps chart colours in sync with the
/// app's Tailwind tokens (see Styles/site.css). ApexCharts options take raw
/// hex strings, so we hard-code the same values here.
/// </summary>
public static class StatsTheme
{
    public const string Accent50  = "#ECFDF5";
    public const string Accent100 = "#D1FAE5";
    public const string Accent200 = "#A7F3D0";
    public const string Accent300 = "#6EE7B7";
    public const string Accent400 = "#34D399";
    public const string Accent500 = "#10B981";
    public const string Accent600 = "#059669";
    public const string Accent700 = "#047857";
    public const string Accent800 = "#065F46";

    public const string Ink100 = "#F5F5F4";
    public const string Ink200 = "#E7E5E4";
    public const string Ink300 = "#D6D3D1";
    public const string Ink400 = "#A8A29E";
    public const string Ink500 = "#78716C";
    public const string Ink700 = "#44403C";
    public const string Ink900 = "#1C1917";

    public const string DangerSoft = "#FECACA"; // red-200
    public const string Danger     = "#DC2626"; // red-600
    public const string DangerDark = "#991B1B"; // red-800

    public const string FontFamily = "Inter, system-ui, sans-serif";
}
