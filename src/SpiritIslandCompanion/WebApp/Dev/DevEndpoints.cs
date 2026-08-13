using System.Security.Claims;
using Application.Features.Games;
using Application.Features.Games.Dtos;
using Application.Features.Players;
using Application.Features.Statistics;
using Application.Features.Users;
using Domain.Models.Game.Enums;
using Domain.Models.Static.Data;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApp.Dev;

/// <summary>
/// Development-only endpoints. Nothing here is mapped outside the Development
/// environment.
///
/// /dev-login?email=…&amp;name=…&amp;returnUrl=…  — signs in without the identity
/// provider: syncs a local user by email (created on first use, same path as
/// OIDC sign-in) and issues the auth cookie directly.
///
/// /dev-seed — fills an EMPTY account with a plausible game history (favourite
/// spirits, recurring adversaries, wins and losses, a few drafts) so the
/// stats-driven screens can be exercised locally. No-op when games exist.
/// </summary>
public static class DevEndpoints
{
    public static WebApplication MapDevEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapGet("/dev-login", async (HttpContext context, IMediator mediator, string email, string? name, string? returnUrl) =>
        {
            var result = await mediator.Send(new SyncUserCommand(email, name ?? email));
            if (result.IsFailure)
                return Results.BadRequest(result.Error.Message);

            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, name ?? email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim("db_user_id", result.Value.UserId.ToString())
                ],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.Redirect(returnUrl ?? "/");
        }).AllowAnonymous();

        app.MapGet("/dev-seed", async (HttpContext context, IMediator mediator) =>
        {
            var claim = context.User.FindFirst("db_user_id")?.Value;
            if (!Guid.TryParse(claim, out var userId))
                return Results.Unauthorized();

            var existing = await mediator.Send(new GetSetupFactsQuery(userId));
            if (existing.IsFailure)
                return Results.BadRequest(existing.Error.Message);
            if (existing.Value.Count > 0)
                return Results.Ok($"Account already has {existing.Value.Count} games — nothing seeded.");

            await mediator.Send(new CreatePlayerCommand("Anna", userId));
            await mediator.Send(new CreatePlayerCommand("Marek", userId));
            var locals = (await mediator.Send(new ListPlayersQuery(userId))).Value;

            var created = await SeedGames(mediator, userId, locals.Select(p => p.Id).ToList());
            return Results.Ok($"Seeded {created} games.");
        }).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// ~28 games over the last 1.5 years with intentional shape: go-to spirits with
    /// board habits, two recurring adversaries at climbing levels, an occasional
    /// scenario, solo and multiplayer tables, and a couple of unfinished drafts.
    /// </summary>
    private static async Task<int> SeedGames(IMediator mediator, Guid userId, IReadOnlyList<Guid> localPlayerIds)
    {
        var rng = new Random(20260801);
        var spirits = GameData.Spirits.Select(s => s.Id.Value).ToList();
        var boards = GameData.Boards.Select(b => b.Id.Value).ToList();
        var adversaries = GameData.Adversaries.ToList();
        var scenarios = GameData.Scenarios.Select(s => s.Id.Value).ToList();

        // Favourite spirits (heavy weights first) and each one's habitual board.
        var favourites = spirits.Take(6).ToList();
        var habitualBoard = favourites
            .Select((id, i) => (id, board: boards[i % Math.Min(4, boards.Count)]))
            .ToDictionary(x => x.id, x => x.board);

        var recurringFoes = adversaries.Take(3).ToList();

        var created = 0;
        for (var i = 0; i < 28; i++)
        {
            var playerCount = rng.Next(100) switch { < 55 => 1, < 85 => 2, _ => 3 };
            playerCount = Math.Min(playerCount, 1 + localPlayerIds.Count);

            var tableSpirits = PickDistinct(rng, favourites, spirits, playerCount);
            var tableBoards = new List<string>();
            var gamePlayers = new List<GamePlayerDto>();
            for (var seat = 0; seat < playerCount; seat++)
            {
                var spirit = tableSpirits[seat];
                var board = habitualBoard.TryGetValue(spirit, out var b) && !tableBoards.Contains(b) && rng.Next(100) < 70
                    ? b
                    : boards.Where(x => !tableBoards.Contains(x)).ElementAt(rng.Next(boards.Count - tableBoards.Count));
                tableBoards.Add(board);
                gamePlayers.Add(seat == 0
                    ? new GamePlayerDto(spirit, null, board, userId, null)
                    : new GamePlayerDto(spirit, null, board, null, localPlayerIds[seat - 1]));
            }

            var gameAdversaries = new List<GameAdversaryDto>();
            if (rng.Next(100) < 80)
            {
                var foe = rng.Next(100) < 75 ? recurringFoes[rng.Next(recurringFoes.Count)] : adversaries[rng.Next(adversaries.Count)];
                var maxLevel = foe.Modes.Max(m => m.Level);
                gameAdversaries.Add(new GameAdversaryDto(foe.Id.Value, rng.Next(maxLevel + 1)));
            }

            var scenarioId = rng.Next(100) < 20 ? scenarios[rng.Next(Math.Min(4, scenarios.Count))] : null;
            // Published only: a hand-built setup id has to be saved together with an
            // arrangement, and the seeder has none to give.
            var setups = GameData.PublishedIslandSetups
                .Where(s => s.NumberOfPlayers == playerCount && !s.IsThematic)
                .ToList();
            var setup = setups[rng.Next(setups.Count)].Id.Value;
            var startedAt = DateTimeOffset.UtcNow.AddDays(-rng.Next(2, 540)).AddHours(rng.Next(10, 22));

            if (i < 25)
            {
                var win = rng.Next(100) < 55;
                var result = new GameResultDto(
                    win,
                    TimeSpan.FromMinutes(rng.Next(45, 150)),
                    Cards: rng.Next(4, 14),
                    (TerrorLevel)rng.Next(0, 4),
                    Blight: rng.Next(1, 8),
                    Dahan: rng.Next(2, 12),
                    ScoreModifier: 0);

                var cmd = new CreateGameCommand(userId, startedAt, setup, false, null, false, 0,
                    gamePlayers, gameAdversaries, scenarioId, result, null);
                if ((await mediator.Send(cmd)).IsSuccess) created++;
            }
            else
            {
                var cmd = new DraftGameCommand(userId, startedAt, setup, false, null, false, 0,
                    gamePlayers, gameAdversaries, scenarioId, null);
                if ((await mediator.Send(cmd)).IsSuccess) created++;
            }
        }

        return created;
    }

    /// <summary>Distinct spirits for one table: favourites (weighted) topped up from the full pool.</summary>
    private static List<string> PickDistinct(Random rng, IReadOnlyList<string> favourites, IReadOnlyList<string> all, int count)
    {
        var picked = new List<string>();
        while (picked.Count < count)
        {
            var pool = rng.Next(100) < 70 ? favourites : all;
            var candidate = pool[rng.Next(pool.Count)];
            if (!picked.Contains(candidate)) picked.Add(candidate);
        }
        return picked;
    }
}
