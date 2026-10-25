namespace TicTacToe.Api.Data.Entities;

public class Move
{
    public int Id { get; set; }
    public Guid GameId { get; set; }
    public Game Game { get; set; } = null!;

    /// <summary>1-based, contiguous within a game.</summary>
    public int MoveNumber { get; set; }
    public Player Player { get; set; }

    /// <summary>0-8, row-major (row * 3 + column).</summary>
    public int CellIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 1-based, for display ("Row 1, Column 1"). Not persisted.
    public int Row => CellIndex / 3 + 1;
    public int Column => CellIndex % 3 + 1;
}
