using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Data.Repositories;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Tests;

public class VsComputerGameTests : IDisposable
{
    private readonly GameHarness _h = new();
    public void Dispose() => _h.Dispose();

    private async Task<Guid> NewGameAsync() => (await _h.CreateAsync(GameMode.VsComputer)).Id;

    [Fact]
    public async Task Computer_replies_immediately_and_it_is_the_humans_turn_again()
    {
        var id = await NewGameAsync();

        var game = await _h.MoveAsync(id, Player.X, 0);

        Assert.Equal(2, game.Moves.Count);
        Assert.Equal(Player.O, game.Moves[1].Player);
        Assert.Equal(Player.O, game.Board[4]);            // no threats, so it takes the center
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public async Task Computer_wins_when_the_human_ignores_its_threat()
    {
        var id = await NewGameAsync();
        await _h.MoveAsync(id, Player.X, 0);              // O 4
        await _h.MoveAsync(id, Player.X, 1);              // O blocks 2
        var game = await _h.MoveAsync(id, Player.X, 8);   // ignores O's 2-4 threat; O plays 6

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.Equal(new[] { 2, 4, 6 }, game.WinningCells);
        Assert.Equal(1, game.Scoreboard.OWins);
    }

    [Fact]
    public async Task Undo_removes_the_computers_move_and_the_humans_move_before_it()
    {
        var id = await NewGameAsync();
        await _h.MoveAsync(id, Player.X, 0);

        var game = await _h.UndoAsync(id);

        Assert.Empty(game.Moves);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public async Task Undo_after_a_computer_win_reopens_the_game_and_reverses_the_score()
    {
        var id = await NewGameAsync();
        await _h.MoveAsync(id, Player.X, 0);
        await _h.MoveAsync(id, Player.X, 1);
        await _h.MoveAsync(id, Player.X, 8);              // computer wins with its 6th move

        var game = await _h.UndoAsync(id);

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(4, game.Moves.Count);                // X0 O4 X1 O2 remain
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(0, game.Scoreboard.OWins);
    }

    [Fact]
    public async Task Human_win_ends_the_game_without_a_computer_reply_and_undo_removes_one_move()
    {
        var id = await NewGameAsync();
        // Human X plays 0, 8, 6, 7 against the computer's 4, 2, 3 and completes 6-7-8.
        await _h.MoveAsync(id, Player.X, 0);
        await _h.MoveAsync(id, Player.X, 8);
        await _h.MoveAsync(id, Player.X, 6);
        var won = await _h.MoveAsync(id, Player.X, 7);

        Assert.Equal(GameStatus.Won, won.Status);
        Assert.Equal(Player.X, won.Winner);
        Assert.Equal(7, won.Moves.Count);                 // last move is the human's
        Assert.Equal(1, won.Scoreboard.XWins);

        var undone = await _h.UndoAsync(id);

        Assert.Equal(6, undone.Moves.Count);              // only the human's move was removed
        Assert.Equal(GameStatus.InProgress, undone.Status);
        Assert.Equal(Player.X, undone.CurrentPlayer);
        Assert.Equal(0, undone.Scoreboard.XWins);
    }

    [Fact]
    public async Task Computer_does_not_move_after_the_game_is_over()
    {
        var id = await NewGameAsync();
        await _h.MoveAsync(id, Player.X, 0);
        await _h.MoveAsync(id, Player.X, 1);
        await _h.MoveAsync(id, Player.X, 8);              // computer wins

        var ex = await Assert.ThrowsAsync<GameException>(() => _h.MoveAsync(id, Player.X, 5));

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(6, (await _h.GetAsync(id)).Moves.Count);
    }

    [Fact]
    public async Task Human_cannot_play_as_O_in_computer_mode()
    {
        var id = await NewGameAsync();

        var ex = await Assert.ThrowsAsync<GameException>(() => _h.MoveAsync(id, Player.O, 0));

        Assert.Equal(409, ex.StatusCode);
    }
}

public class ScoreboardRepositoryTests : IDisposable
{
    private readonly GameHarness _h = new();
    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Recording_a_finished_game_again_does_not_change_the_scoreboard()
    {
        var game = await _h.CreateAsync(GameMode.TwoPlayer);
        await _h.PlayAsync(game.Id, 0, 3, 1, 4, 2);       // X wins

        using var scope = _h.CreateScope();
        var games = (IGameRepository)scope.ServiceProvider.GetService(typeof(IGameRepository))!;
        var scores = (IScoreboardRepository)scope.ServiceProvider.GetService(typeof(IScoreboardRepository))!;
        var entity = await games.GetAsync(game.Id);

        var recordedAgain = await scores.RecordResultAsync(entity!);

        Assert.False(recordedAgain);
        Assert.Equal(1, (await _h.ScoreAsync()).XWins);
    }
}
