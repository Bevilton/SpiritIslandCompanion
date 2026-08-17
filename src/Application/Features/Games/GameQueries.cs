using Domain.Models.Game;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>Query shapes shared by every reader of a user's game history.</summary>
internal static class GameQueries
{
    /// <summary>
    /// The games a user was part of — as the owner, or seated as a registered player. The
    /// game list, the statistics page and the setup facts all answer over this same set;
    /// the shape lives once so the three can't drift apart.
    /// <para>
    /// The setup aggregates are owned types, so EF loads them with each game without any
    /// Includes — among them the stored island geometry, which none of the readers use.
    /// Project server-side here if that blob ever gets heavy enough to matter.
    /// </para>
    /// </summary>
    public static IQueryable<Game> InvolvingUser(this IQueryable<Game> games, Guid userId) =>
        games
            .AsNoTracking()
            .Where(g => g.OwnerId.Value == userId ||
                        g.Players.Any(p => p.UserId != null && p.UserId.Value == userId));
}
