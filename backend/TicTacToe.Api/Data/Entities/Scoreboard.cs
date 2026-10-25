namespace TicTacToe.Api.Data.Entities;

/// <summary>Running totals for one game mode. There is one row per <see cref="GameMode"/>.</summary>
public class Scoreboard : IVersioned
{
    public GameMode Mode { get; set; }
    public int XWins { get; set; }
    public int OWins { get; set; }
    public int Draws { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}
