using Microsoft.EntityFrameworkCore;
using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Data.Repositories;

public sealed class GameRepository(AppDbContext db) : IGameRepository
{
    public Task<Game?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Games
            .Include(g => g.Moves.OrderBy(m => m.MoveNumber))
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public Game Create(GameMode mode)
    {
        var game = new Game { Mode = mode };
        db.Games.Add(game);
        return game;
    }

    public void AddMove(Game game, Player player, int cellIndex)
    {
        game.Moves.Add(new Move
        {
            GameId = game.Id,
            MoveNumber = game.Moves.Count + 1,
            Player = player,
            CellIndex = cellIndex
        });
    }

    public IReadOnlyList<Move> RemoveLastMoves(Game game, int count)
    {
        var removed = game.Moves
            .OrderByDescending(m => m.MoveNumber)
            .Take(count)
            .ToList();

        foreach (var move in removed)
        {
            game.Moves.Remove(move);
            db.Moves.Remove(move);
        }

        return removed;
    }

    public void Reset(Game game)
    {
        db.Moves.RemoveRange(game.Moves);
        game.Moves.Clear();

        game.Status = GameStatus.InProgress;
        game.CurrentPlayer = Player.X;
        game.Winner = null;
        game.WinningCells = null;

        // Forget the scoring marker WITHOUT reverting the scoreboard: the finished game
        // already counted, and Reset must leave totals unchanged.
        game.RecordedOutcome = null;
    }
}
