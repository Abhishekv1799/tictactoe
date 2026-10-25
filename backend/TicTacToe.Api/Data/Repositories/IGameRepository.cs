using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Data.Repositories;

/// <summary>
/// Persistence operations for the Game aggregate. Methods change tracked entities only;
/// nothing is written until <see cref="IUnitOfWork.CommitAsync"/>. Game rules (win
/// detection, turn order, computer moves) belong to the service layer, not here.
/// </summary>
public interface IGameRepository
{
    /// <summary>Loads a game with its moves ordered by MoveNumber, or null if not found.</summary>
    Task<Game?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new in-progress game (X to move) and tracks it.</summary>
    Game Create(GameMode mode);

    /// <summary>Appends a move to the game's history.</summary>
    void AddMove(Game game, Player player, int cellIndex);

    /// <summary>Removes the latest <paramref name="count"/> moves and returns them (newest first).</summary>
    IReadOnlyList<Move> RemoveLastMoves(Game game, int count);

    /// <summary>
    /// Clears the moves and returns the game to a fresh state (InProgress, X to move, no
    /// winner). Deliberately does NOT touch the scoreboard, so totals survive a reset.
    /// </summary>
    void Reset(Game game);
}
