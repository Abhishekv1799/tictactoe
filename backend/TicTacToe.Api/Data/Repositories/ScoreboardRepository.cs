using Microsoft.EntityFrameworkCore;
using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Data.Repositories;

public sealed class ScoreboardRepository(AppDbContext db) : IScoreboardRepository
{
    public Task<Scoreboard> GetAsync(GameMode mode, CancellationToken ct = default) =>
        db.Scoreboards.SingleAsync(s => s.Mode == mode, ct);

    public async Task<bool> RecordResultAsync(Game game, CancellationToken ct = default)
    {
        if (game.RecordedOutcome is not null)
            return false;

        ScoreOutcome? outcome = game.Status switch
        {
            GameStatus.Won => game.Winner == Player.X ? ScoreOutcome.XWin : ScoreOutcome.OWin,
            GameStatus.Draw => ScoreOutcome.Draw,
            _ => null
        };
        if (outcome is null)
            return false;

        Apply(await GetAsync(game.Mode, ct), outcome.Value, +1);
        game.RecordedOutcome = outcome;
        return true;
    }

    public async Task<bool> RevertResultAsync(Game game, CancellationToken ct = default)
    {
        if (game.RecordedOutcome is not { } outcome)
            return false;

        Apply(await GetAsync(game.Mode, ct), outcome, -1);
        game.RecordedOutcome = null;
        return true;
    }

    public async Task ResetAsync(GameMode mode, CancellationToken ct = default)
    {
        var scoreboard = await GetAsync(mode, ct);
        scoreboard.XWins = 0;
        scoreboard.OWins = 0;
        scoreboard.Draws = 0;
    }

    private static void Apply(Scoreboard s, ScoreOutcome outcome, int delta)
    {
        switch (outcome)
        {
            case ScoreOutcome.XWin: s.XWins = Math.Max(0, s.XWins + delta); break;
            case ScoreOutcome.OWin: s.OWins = Math.Max(0, s.OWins + delta); break;
            case ScoreOutcome.Draw: s.Draws = Math.Max(0, s.Draws + delta); break;
        }
    }
}
