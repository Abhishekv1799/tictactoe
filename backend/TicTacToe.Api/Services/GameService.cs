using TicTacToe.Api.Contracts;
using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Data.Repositories;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Services;

public sealed class GameService(
    IGameRepository games,
    IScoreboardRepository scoreboards,
    IUnitOfWork unitOfWork)
{
    public async Task<GameResponse> CreateAsync(GameMode mode, CancellationToken ct)
    {
        if (!Enum.IsDefined(mode))
            throw GameException.BadRequest("Mode must be TwoPlayer or VsComputer.");

        var game = games.Create(mode);
        await unitOfWork.CommitAsync(ct);
        return await ToResponseAsync(game, ct);
    }

    public async Task<GameResponse> GetAsync(Guid id, CancellationToken ct) =>
        await ToResponseAsync(await LoadAsync(id, ct), ct);

    public async Task<GameResponse> MakeMoveAsync(Guid id, MoveRequest request, CancellationToken ct)
    {
        var game = await LoadAsync(id, ct);

        if (game.Status != GameStatus.InProgress)
            throw GameException.Conflict("The game is already over. Reset it to play again.");
        if (!Enum.IsDefined(request.Player))
            throw GameException.BadRequest("Player must be X or O.");
        if (request.Row is < 1 or > 3 || request.Column is < 1 or > 3)
            throw GameException.BadRequest("Row and column must each be between 1 and 3.");
        if (request.Player != game.CurrentPlayer)
            throw GameException.Conflict($"It is {game.CurrentPlayer}'s turn.");

        var cell = (request.Row - 1) * 3 + (request.Column - 1);
        if (game.BuildBoard()[cell] is not null)
            throw GameException.Conflict("That cell is already taken.");

        games.AddMove(game, request.Player, cell);
        Evaluate(game);

        // Computer (always O) replies immediately unless the human's move ended the game.
        if (game.Status == GameStatus.InProgress
            && game.Mode == GameMode.VsComputer
            && game.CurrentPlayer == Player.O)
        {
            var computerCell = GameRules.ChooseComputerMove(game.BuildBoard());
            games.AddMove(game, Player.O, computerCell);
            Evaluate(game);
        }

        if (game.Status != GameStatus.InProgress)
            await scoreboards.RecordResultAsync(game, ct);   // no-op if already recorded

        await unitOfWork.CommitAsync(ct);
        return await ToResponseAsync(game, ct);
    }

    public async Task<GameResponse> UndoAsync(Guid id, CancellationToken ct)
    {
        var game = await LoadAsync(id, ct);

        if (game.Moves.Count == 0)
            throw GameException.Conflict("There are no moves to undo.");

        var last = game.Moves.OrderBy(m => m.MoveNumber).Last();

        // TwoPlayer: one move. VsComputer: the computer's move plus the human move before it.
        // (If the human's move ended the game there is no computer reply, so only one move.)
        var count = game.Mode == GameMode.VsComputer && last.Player == Player.O && game.Moves.Count >= 2
            ? 2
            : 1;

        await scoreboards.RevertResultAsync(game, ct);       // undoing a finished game un-scores it
        games.RemoveLastMoves(game, count);
        Evaluate(game);

        await unitOfWork.CommitAsync(ct);
        return await ToResponseAsync(game, ct);
    }

    public async Task<GameResponse> ResetAsync(Guid id, CancellationToken ct)
    {
        var game = await LoadAsync(id, ct);
        games.Reset(game);                                   // scoreboard is intentionally untouched
        await unitOfWork.CommitAsync(ct);
        return await ToResponseAsync(game, ct);
    }

    /// <summary>
    /// Recomputes status, winner and current player purely from the move list, so the
    /// same code path serves new moves and undo.
    /// </summary>
    private static void Evaluate(Game game)
    {
        var board = game.BuildBoard();

        if (GameRules.FindWinner(board) is { } win)
        {
            game.Status = GameStatus.Won;
            game.Winner = win.Winner;
            game.WinningCells = win.Cells.ToArray();
        }
        else
        {
            game.Status = GameRules.IsFull(board) ? GameStatus.Draw : GameStatus.InProgress;
            game.Winner = null;
            game.WinningCells = null;
        }

        game.CurrentPlayer = game.Moves.Count % 2 == 0 ? Player.X : Player.O;
        game.UpdatedAt = DateTime.UtcNow;   // marks the game modified so its version always bumps
    }

    private async Task<Game> LoadAsync(Guid id, CancellationToken ct) =>
        await games.GetAsync(id, ct) ?? throw GameException.NotFound(id);

    private async Task<GameResponse> ToResponseAsync(Game game, CancellationToken ct) =>
        GameMapper.ToResponse(game, await scoreboards.GetAsync(game.Mode, ct));
}
