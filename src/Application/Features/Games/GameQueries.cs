using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>Query shapes shared by every reader of a user's game history.</summary>
internal static class GameQueries
{
    /// <summary>
    /// The games a user was part of — as the owner, or seated as a registered player. The
    /// game list, the statistics page and the setup facts all answer over this same set;
    /// the shape lives once so the three can't drift apart. Untracked: this is a reader's
    /// set, and nothing that changes a game asks over it.
    /// <para>
    /// The seats, adversaries and outcome are owned by the game, so EF loads them with it
    /// without any Includes — and so is the stored island geometry, which none of the readers
    /// use. Project server-side here if that blob ever gets heavy enough to matter.
    /// </para>
    /// </summary>
    public static IQueryable<Game> InvolvingUser(this IQueryable<Game> games, UserId userId) =>
        games
            .AsNoTracking()
            .Where(g => g.OwnerId == userId || g.Players.Any(p => p.UserId == userId));

    /// <summary>
    /// The games with a given local player in a seat. Tracking is the caller's call: the
    /// merge and the delete rewrite these seats, the merge's preview only reads them.
    /// </summary>
    public static IQueryable<Game> SeatingLocalPlayer(this IQueryable<Game> games, PlayerId playerId) =>
        games.Where(g => g.Players.Any(p => p.PlayerId == playerId));
}
