namespace TicTacToe.Api.Data.Entities;

public class Game : IVersioned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public GameMode Mode { get; set; }
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public Player CurrentPlayer { get; set; } = Player.X;

    /// <summary>Set only when <see cref="Status"/> is Won.</summary>
    public Player? Winner { get; set; }

    /// <summary>Cell indexes (0-8) of the winning line. Set only when Status is Won.</summary>
    public int[]? WinningCells { get; set; }

    /// <summary>
    /// What this game contributed to the scoreboard, or null if nothing yet.
    /// This is the guard that makes scoring happen exactly once per completed game,
    /// and it lets Undo reverse precisely what was added.
    /// </summary>
    public ScoreOutcome? RecordedOutcome { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;

    public List<Move> Moves { get; set; } = [];

    /// <summary>
    /// The board is derived from the move list (single source of truth), so Undo can
    /// never leave the board and history out of sync. Index = row * 3 + column.
    /// </summary>
    public Player?[] BuildBoard()
    {
        var board = new Player?[9];
        foreach (var move in Moves)
            board[move.CellIndex] = move.Player;
        return board;
    }
}
