using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Tests;

public class GameRulesTests
{
    /// <summary>Parses a 9-character board: X, O or '.' (empty), row-major.</summary>
    private static Player?[] Board(string cells) =>
        cells.Select(c => c switch { 'X' => (Player?)Player.X, 'O' => Player.O, _ => null }).ToArray();

    public static TheoryData<int[]> AllLines => new()
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 },
    };

    [Theory]
    [MemberData(nameof(AllLines))]
    public void FindWinner_detects_every_row_column_and_diagonal(int[] line)
    {
        var board = new Player?[9];
        foreach (var i in line) board[i] = Player.O;

        var result = GameRules.FindWinner(board);

        Assert.NotNull(result);
        Assert.Equal(Player.O, result!.Value.Winner);
        Assert.Equal(line, result.Value.Cells);
    }

    [Theory]
    [InlineData(".........")]
    [InlineData("XOX.O.OX.")]
    [InlineData("XOXXOOOXX")]   // full board, no line
    public void FindWinner_returns_null_when_nobody_has_a_line(string cells) =>
        Assert.Null(GameRules.FindWinner(Board(cells)));

    [Fact]
    public void IsFull_is_true_only_when_all_nine_cells_are_taken()
    {
        Assert.True(GameRules.IsFull(Board("XOXXOOOXX")));
        Assert.False(GameRules.IsFull(Board("XOXXOOOX.")));
    }

    // ---- computer (O) move priority: win, block, center, corner, any ----

    [Fact]
    public void Computer_takes_the_winning_move_over_blocking()
    {
        // O can win at 2; X could win at 5. Winning comes first.
        Assert.Equal(2, GameRules.ChooseComputerMove(Board("OO.XX....")));
    }

    [Fact]
    public void Computer_blocks_when_X_is_about_to_win()
    {
        Assert.Equal(2, GameRules.ChooseComputerMove(Board("XX.O.....")));
    }

    [Fact]
    public void Computer_takes_center_when_free_and_no_threats()
    {
        Assert.Equal(4, GameRules.ChooseComputerMove(Board("X........")));
    }

    [Fact]
    public void Computer_takes_first_free_corner_when_center_is_taken()
    {
        Assert.Equal(0, GameRules.ChooseComputerMove(Board("....X....")));
        Assert.Equal(2, GameRules.ChooseComputerMove(Board("X...O....")));
    }

    [Fact]
    public void Computer_falls_back_to_any_free_cell()
    {
        // Center and all corners taken, no threats for either side: free cells are 3 and 5.
        Assert.Equal(3, GameRules.ChooseComputerMove(Board("XOX.X.OXO")));
    }

    [Fact]
    public void Computer_returns_minus_one_on_a_full_board() =>
        Assert.Equal(-1, GameRules.ChooseComputerMove(Board("XOXXOOOXX")));

    [Fact]
    public void Computer_never_modifies_the_board_it_is_given()
    {
        var board = Board("XX.O.....");
        var before = board.ToArray();

        GameRules.ChooseComputerMove(board);

        Assert.Equal(before, board);
    }
}
