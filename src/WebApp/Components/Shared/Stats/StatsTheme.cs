namespace WebApp.Components.Shared.Stats;

/// <summary>
/// Shared colour palette for ApexCharts. Keeps chart colours in sync with the
/// app's Tailwind tokens (see Styles/site.css). ApexCharts options take raw
/// hex strings, so we hard-code the same values here.
/// </summary>
public static class StatsTheme
{
    // accent — jade
    public const string Accent50  = "#EDF7F1";
    public const string Accent100 = "#D3EFE0";
    public const string Accent200 = "#A9E0C5";
    public const string Accent300 = "#74CCA6";
    public const string Accent400 = "#3BB588";
    public const string Accent500 = "#1FA877";
    public const string Accent600 = "#15875F";
    public const string Accent700 = "#0F6A4A";
    public const string Accent800 = "#0B5138";

    // ink — parchment neutrals. Grid lines and axis labels sit on a card, so they are
    // the light end of the ramp; anything carrying text is 500 or darker for contrast.
    public const string Ink100 = "#F0EADC";
    public const string Ink200 = "#E3DAC6";
    public const string Ink300 = "#CFC2A5";
    public const string Ink400 = "#A89878";
    public const string Ink500 = "#7A6C4E";
    public const string Ink700 = "#4A4031";
    public const string Ink900 = "#221D16";

    // ember — the invaders. What a loss is drawn in.
    public const string DangerSoft = "#F4A98A"; // ember-300
    public const string Danger     = "#DC5B3C"; // ember-500
    public const string DangerDark = "#8F3020"; // ember-700

    // sun — score, difficulty, terror
    public const string Sun300 = "#F2D36B";
    public const string Sun400 = "#E6C341";
    public const string Sun500 = "#D4AC1E";
    public const string Sun600 = "#A8850F";

    public const string FontFamily = "Inter, system-ui, sans-serif";
}
