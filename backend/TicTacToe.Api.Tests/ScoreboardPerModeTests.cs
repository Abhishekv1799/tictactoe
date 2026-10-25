using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Tests;

/// <summary>Two Player and Vs Computer each keep their own scoreboard.</summary>
public class ScoreboardPerModeTests : IDisposable
{
    private readonly GameHarness _h = new();
    public void Dispose() => _h.Dispose();

    // Two Player: X wins the top row.
    private async Task PlayTwoPlayerXWinAsync()
    {
        var game = await _h.CreateAsync(GameMode.TwoPlayer);
        await _h.PlayAsync(game.Id, 0, 3, 1, 4, 2);
    }

    // Vs Computer: the human ignores a threat and the computer (O) wins.
    private async Task PlayComputerWinAsync()
    {
        var game = await _h.CreateAsync(GameMode.VsComputer);
        await _h.MoveAsync(game.Id, Player.X, 0);
        await _h.MoveAsync(game.Id, Player.X, 1);
        await _h.MoveAsync(game.Id, Player.X, 8);
    }

    [Fact]
    public async Task A_two_player_result_does_not_change_the_computer_scoreboard()
    {
        await PlayTwoPlayerXWinAsync();

        var twoPlayer = await _h.ScoreAsync(GameMode.TwoPlayer);
        var vsComputer = await _h.ScoreAsync(GameMode.VsComputer);

        Assert.Equal((1, 0, 0), (twoPlayer.XWins, twoPlayer.OWins, twoPlayer.Draws));
        Assert.Equal((0, 0, 0), (vsComputer.XWins, vsComputer.OWins, vsComputer.Draws));
    }

    [Fact]
    public async Task A_computer_game_result_does_not_change_the_two_player_scoreboard()
    {
        await PlayComputerWinAsync();

        var twoPlayer = await _h.ScoreAsync(GameMode.TwoPlayer);
        var vsComputer = await _h.ScoreAsync(GameMode.VsComputer);

        Assert.Equal(1, vsComputer.OWins);
        Assert.Equal((0, 0, 0), (twoPlayer.XWins, twoPlayer.OWins, twoPlayer.Draws));
    }

    [Fact]
    public async Task Each_game_response_carries_its_own_modes_scoreboard()
    {
        await PlayComputerWinAsync();

        var twoPlayerGame = await _h.CreateAsync(GameMode.TwoPlayer);
        var computerGame = await _h.CreateAsync(GameMode.VsComputer);

        Assert.Equal(0, twoPlayerGame.Scoreboard.OWins);
        Assert.Equal(1, computerGame.Scoreboard.OWins);
    }

    [Fact]
    public async Task Resetting_one_scoreboard_leaves_the_other_untouched()
    {
        await PlayTwoPlayerXWinAsync();
        await PlayComputerWinAsync();

        var reset = await _h.ResetScoreAsync(GameMode.VsComputer);

        Assert.Equal((0, 0, 0), (reset.XWins, reset.OWins, reset.Draws));
        Assert.Equal(1, (await _h.ScoreAsync(GameMode.TwoPlayer)).XWins);
    }

    [Fact]
    public async Task Undoing_a_computer_win_only_adjusts_the_computer_scoreboard()
    {
        await PlayTwoPlayerXWinAsync();
        var game = await _h.CreateAsync(GameMode.VsComputer);
        await _h.MoveAsync(game.Id, Player.X, 0);
        await _h.MoveAsync(game.Id, Player.X, 1);
        await _h.MoveAsync(game.Id, Player.X, 8);          // computer wins

        await _h.UndoAsync(game.Id);

        Assert.Equal(0, (await _h.ScoreAsync(GameMode.VsComputer)).OWins);
        Assert.Equal(1, (await _h.ScoreAsync(GameMode.TwoPlayer)).XWins);
    }
}
