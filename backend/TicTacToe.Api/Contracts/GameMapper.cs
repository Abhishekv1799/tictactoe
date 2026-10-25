using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Contracts;

public static class GameMapper
{
    public static ScoreboardDto ToDto(Scoreboard s) => new(s.XWins, s.OWins, s.Draws);

    public static GameResponse ToResponse(Game game, Scoreboard scoreboard) => new(
        game.Id,
        game.Mode,
        game.Status,
        game.CurrentPlayer,
        game.Winner,
        game.WinningCells,
        game.BuildBoard(),
        game.Moves
            .OrderBy(m => m.MoveNumber)
            .Select(m => new MoveDto(m.MoveNumber, m.Player, m.Row, m.Column, m.CellIndex))
            .ToList(),
        ToDto(scoreboard));
}
