using Application.Features.Games.Dtos;
using Domain.Models.Game.Enums;
using Domain.Services;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// A game's outcome while it is still being entered — the seven numbers
/// <see cref="GameResultEditor"/> asks for, the score they add up to, and the DTO they are saved
/// as.
/// <para>
/// The two screens that record an outcome — the setup form's optional result panel and a draft's
/// "complete game" flow — each held the seven as separate fields, wired each one to the editor as
/// a value/callback pair (fourteen parameters), and then computed the provisional score by
/// spelling the seven arguments out a third time. Held together they are one object the page owns
/// and the editor edits, which is what they always were: the editor's parameter list is what the
/// duplication had grown into, not what the control needs.
/// </para>
/// <para>
/// Mutable and passed by reference on purpose. The editor writes straight into the page's own
/// draft and raises <see cref="GameResultEditor.OnChanged"/>, which is what re-renders the page
/// around it — the action bar's live score, the outcome's tone.
/// </para>
/// </summary>
public sealed class GameResultDraft
{
    /// <summary>Victory by default: most recorded games are wins, and the alternative is one tap.</summary>
    public bool Win { get; set; } = true;

    public TerrorLevel TerrorLevel { get; set; } = TerrorLevel.First;

    /// <summary>An hour, the length of a short game — a plausible figure to correct rather than
    /// a zero to fill in.</summary>
    public int DurationMinutes { get; set; } = 60;

    public int Dahan { get; set; }
    public int Blight { get; set; }

    /// <summary>Cards left undrawn on a victory, cards used on a defeat — the label the editor
    /// puts on this field follows <see cref="Win"/>.</summary>
    public int Cards { get; set; }

    public int ScoreModifier { get; set; }

    /// <summary>
    /// What this outcome scores at the table it was played on — the same calculation the server
    /// runs when the game is saved, so the preview and the recorded score agree.
    /// </summary>
    public int Score(int difficulty, int playerCount) =>
        ScoreCalculator.Compute(
            win: Win,
            difficulty: difficulty,
            dahan: Dahan,
            cards: Cards,
            blight: Blight,
            playerCount: playerCount,
            scoreModifier: ScoreModifier);

    /// <summary>The outcome as the create / complete commands take it.</summary>
    public GameResultDto ToDto() =>
        new(Win, TimeSpan.FromMinutes(DurationMinutes), Cards, TerrorLevel, Blight, Dahan, ScoreModifier);
}
