using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Contracts;

/// <param name="Mode">TwoPlayer or VsComputer (human is X, computer is O).</param>
public record CreateGameRequest(GameMode Mode);

/// <param name="Player">X or O. Must be the player whose turn it is.</param>
/// <param name="Row">1-3, top to bottom.</param>
/// <param name="Column">1-3, left to right.</param>
public record MoveRequest(Player Player, int Row, int Column);

public record MoveDto(int MoveNumber, Player Player, int Row, int Column, int CellIndex);

public record ScoreboardDto(int XWins, int OWins, int Draws);

/// <param name="Board">9 cells, row-major (index = (row-1)*3 + (column-1)); null = empty.</param>
/// <param name="WinningCells">Indexes of the winning line when Status is Won, otherwise null.</param>
public record GameResponse(
    Guid Id,
    GameMode Mode,
    GameStatus Status,
    Player CurrentPlayer,
    Player? Winner,
    int[]? WinningCells,
    Player?[] Board,
    IReadOnlyList<MoveDto> Moves,
    ScoreboardDto Scoreboard);
