using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Tests;

public class TwoPlayerGameTests : IDisposable
{
    private readonly GameHarness _h = new();
    public void Dispose() => _h.Dispose();

    private async Task<Guid> NewGameAsync() => (await _h.CreateAsync(GameMode.TwoPlayer)).Id;

    // X wins the top row: X0 O3 X1 O4 X2
    private static readonly int[] XWinsTopRow = [0, 3, 1, 4, 2];

    // Full board, no winner: X0 O1 X2 O4 X3 O5 X7 O6 X8
    private static readonly int[] DrawGame = [0, 1, 2, 4, 3, 5, 7, 6, 8];

    [Fact]
    public async Task New_game_starts_empty_with_X_to_move()
    {
        var game = await _h.CreateAsync(GameMode.TwoPlayer);

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Empty(game.Moves);
    }

    [Fact]
    public async Task Valid_moves_alternate_players_and_fill_the_board()
    {
        var id = await NewGameAsync();

        var afterX = await _h.MoveAsync(id, Player.X, 4);
        Assert.Equal(Player.X, afterX.Board[4]);
        Assert.Equal(Player.O, afterX.CurrentPlayer);

        var afterO = await _h.MoveAsync(id, Player.O, 0);
        Assert.Equal(Player.O, afterO.Board[0]);
        Assert.Equal(Player.X, afterO.CurrentPlayer);
    }

    [Fact]
    public async Task Move_history_records_number_player_and_position()
    {
        var id = await NewGameAsync();
        await _h.MoveAsync(id, Player.X, 0);
        var game = await _h.MoveAsync(id, Player.O, 4);

        Assert.Collection(game.Moves,
            m => { Assert.Equal(1, m.MoveNumber); Assert.Equal(Player.X, m.Player); Assert.Equal((1, 1), (m.Row, m.Column)); },
            m => { Assert.Equal(2, m.MoveNumber); Assert.Equal(Player.O, m.Player); Assert.Equal((2, 2), (m.Row, m.Column)); });
    }

    [Fact]
    public async Task Occupied_cell_is_rejected_and_the_turn_does_not_change()
    {
        var id = await NewGameAsync();
        await _h.MoveAsync(id, Player.X, 4);

        var ex = await Assert.ThrowsAsync<GameException>(() => _h.MoveAsync(id, Player.O, 4));

        Assert.Equal(409, ex.StatusCode);
        var game = await _h.GetAsync(id);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Single(game.Moves);
    }

    [Fact]
    public async Task Playing_out_of_turn_is_rejected()
    {
        var id = await NewGameAsync();

        var ex = await Assert.ThrowsAsync<GameException>(() => _h.MoveAsync(id, Player.O, 0));

        Assert.Equal(409, ex.StatusCode);
        Assert.Empty((await _h.GetAsync(id)).Moves);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(4, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 4)]
    public async Task Move_outside_the_board_is_rejected(int row, int column)
    {
        var id = await NewGameAsync();
        using var scope = _h.CreateScope();
        var svc = scope.ServiceProvider.GetService(typeof(GameService)) as GameService;

        var ex = await Assert.ThrowsAsync<GameException>(() =>
            svc!.MakeMoveAsync(id, new(Player.X, row, column), CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Unknown_game_returns_404()
    {
        var ex = await Assert.ThrowsAsync<GameException>(() => _h.GetAsync(Guid.NewGuid()));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task Completing_a_line_wins_highlights_cells_and_scores_X()
    {
        var id = await NewGameAsync();

        var game = await _h.PlayAsync(id, XWinsTopRow);

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, game.WinningCells);
        Assert.Equal(1, game.Scoreboard.XWins);
        Assert.Equal(0, game.Scoreboard.OWins);
    }

    [Fact]
    public async Task No_moves_are_accepted_after_a_win_and_the_score_is_counted_once()
    {
        var id = await NewGameAsync();
        await _h.PlayAsync(id, XWinsTopRow);

        var ex = await Assert.ThrowsAsync<GameException>(() => _h.MoveAsync(id, Player.O, 8));

        Assert.Equal(409, ex.StatusCode);
        await _h.GetAsync(id);                       // reading must not score again
        Assert.Equal(1, (await _h.ScoreAsync()).XWins);
    }

    [Fact]
    public async Task Filling_the_board_without_a_line_is_a_draw_and_scores_a_draw()
    {
        var id = await NewGameAsync();

        var game = await _h.PlayAsync(id, DrawGame);

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Null(game.WinningCells);
        Assert.Equal(1, game.Scoreboard.Draws);
        await Assert.ThrowsAsync<GameException>(() => _h.MoveAsync(id, Player.X, 0));
    }

    [Fact]
    public async Task Undo_removes_only_the_latest_move_and_restores_the_turn()
    {
        var id = await NewGameAsync();
        await _h.PlayAsync(id, 0, 4);                // X then O

        var game = await _h.UndoAsync(id);

        Assert.Single(game.Moves);
        Assert.Null(game.Board[4]);
        Assert.Equal(Player.O, game.CurrentPlayer);  // O's turn again
    }

    [Fact]
    public async Task Undo_with_no_moves_is_rejected()
    {
        var id = await NewGameAsync();

        var ex = await Assert.ThrowsAsync<GameException>(() => _h.UndoAsync(id));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Undoing_the_winning_move_reopens_the_game_and_reverses_the_score()
    {
        var id = await NewGameAsync();
        await _h.PlayAsync(id, XWinsTopRow);

        var game = await _h.UndoAsync(id);

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(0, game.Scoreboard.XWins);

        // Replaying the winning move scores exactly once again.
        var replay = await _h.MoveAsync(id, Player.X, 2);
        Assert.Equal(GameStatus.Won, replay.Status);
        Assert.Equal(1, replay.Scoreboard.XWins);
    }

    [Fact]
    public async Task Reset_clears_the_game_but_keeps_the_scoreboard()
    {
        var id = await NewGameAsync();
        await _h.PlayAsync(id, XWinsTopRow);

        var game = await _h.ResetAsync(id);

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.Moves);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Equal(1, game.Scoreboard.XWins);
    }

    [Fact]
    public async Task Replaying_after_reset_adds_a_second_win_to_the_scoreboard()
    {
        var id = await NewGameAsync();
        await _h.PlayAsync(id, XWinsTopRow);
        await _h.ResetAsync(id);

        await _h.PlayAsync(id, XWinsTopRow);

        Assert.Equal(2, (await _h.ScoreAsync()).XWins);
    }

    [Fact]
    public async Task Reset_scoreboard_zeroes_all_totals()
    {
        var id = await NewGameAsync();
        await _h.PlayAsync(id, XWinsTopRow);

        var score = await _h.ResetScoreAsync();

        Assert.Equal((0, 0, 0), (score.XWins, score.OWins, score.Draws));
    }
}
