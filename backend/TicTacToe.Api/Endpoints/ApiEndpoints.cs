using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapApi(this WebApplication app)
    {
        var games = app.MapGroup("/api/games").WithTags("Games");

        games.MapPost("/", async (CreateGameRequest request, GameService svc, CancellationToken ct) =>
        {
            var game = await svc.CreateAsync(request.Mode, ct);
            return Results.Created($"/api/games/{game.Id}", game);
        }).WithSummary("Create a new game session");

        games.MapGet("/{id:guid}", async (Guid id, GameService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)))
            .WithSummary("Get the current game state");

        games.MapPost("/{id:guid}/moves", async (Guid id, MoveRequest request, GameService svc, CancellationToken ct) =>
            Results.Ok(await svc.MakeMoveAsync(id, request, ct)))
            .WithSummary("Submit a move (the computer replies automatically in VsComputer mode)");

        games.MapPost("/{id:guid}/undo", async (Guid id, GameService svc, CancellationToken ct) =>
            Results.Ok(await svc.UndoAsync(id, ct)))
            .WithSummary("Undo the last move (or move pair in VsComputer mode)");

        games.MapPost("/{id:guid}/reset", async (Guid id, GameService svc, CancellationToken ct) =>
            Results.Ok(await svc.ResetAsync(id, ct)))
            .WithSummary("Reset the game; the scoreboard is unchanged");

        var scoreboard = app.MapGroup("/api/scoreboard").WithTags("Scoreboard");

        scoreboard.MapGet("/", async ([FromQuery] GameMode mode, ScoreboardService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(mode, ct)))
            .WithSummary("Get the scoreboard for one mode (?mode=TwoPlayer or VsComputer)");

        scoreboard.MapPost("/reset", async ([FromQuery] GameMode mode, ScoreboardService svc, CancellationToken ct) =>
            Results.Ok(await svc.ResetAsync(mode, ct)))
            .WithSummary("Reset one mode's scoreboard to zero; the other mode is unchanged");
    }
}
