using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Data.Repositories;

/// <summary>Each game mode (TwoPlayer, VsComputer) has its own independent scoreboard.</summary>
public interface IScoreboardRepository
{
    Task<Scoreboard> GetAsync(GameMode mode, CancellationToken ct = default);

    /// <summary>
    /// Adds a finished game to its mode's scoreboard exactly once. Safe to call repeatedly:
    /// returns false (and changes nothing) if the game is still in progress or was
    /// already recorded.
    /// </summary>
    Task<bool> RecordResultAsync(Game game, CancellationToken ct = default);

    /// <summary>
    /// Reverses whatever <see cref="RecordResultAsync"/> added for this game (used by
    /// Undo on a completed game). Uses the stored outcome, so it can be called before or
    /// after the game's status changes. Returns false if nothing had been recorded.
    /// </summary>
    Task<bool> RevertResultAsync(Game game, CancellationToken ct = default);

    /// <summary>Zeroes the totals for one mode; the other mode is untouched.</summary>
    Task ResetAsync(GameMode mode, CancellationToken ct = default);
}
