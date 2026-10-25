using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Domain;

/// <summary>Pure Tic Tac Toe rules. Boards are 9 cells, index = row * 3 + column.</summary>
public static class GameRules
{
    private static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],   // rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8],   // columns
        [0, 4, 8], [2, 4, 6]               // diagonals
    ];

    private static readonly int[] Corners = [0, 2, 6, 8];

    public static (Player Winner, int[] Cells)? FindWinner(Player?[] board)
    {
        foreach (var line in Lines)
        {
            var p = board[line[0]];
            if (p is not null && board[line[1]] == p && board[line[2]] == p)
                return (p.Value, line);
        }
        return null;
    }

    public static bool IsFull(Player?[] board) => board.All(c => c is not null);

    /// <summary>
    /// Computer (O) move priority: 1) win, 2) block X, 3) center, 4) corner, 5) any cell.
    /// Returns -1 if the board is full.
    /// </summary>
    public static int ChooseComputerMove(Player?[] board)
    {
        var empty = Enumerable.Range(0, 9).Where(i => board[i] is null).ToList();
        if (empty.Count == 0) return -1;

        var win = FindCompletingCell(board, Player.O, empty);
        if (win is not null) return win.Value;

        var block = FindCompletingCell(board, Player.X, empty);
        if (block is not null) return block.Value;

        if (board[4] is null) return 4;

        foreach (var corner in Corners)
            if (board[corner] is null) return corner;

        return empty[0];
    }

    /// <summary>First empty cell where placing <paramref name="player"/> wins the game.</summary>
    private static int? FindCompletingCell(Player?[] board, Player player, List<int> empty)
    {
        var copy = (Player?[])board.Clone();
        foreach (var cell in empty)
        {
            copy[cell] = player;
            var result = FindWinner(copy);
            copy[cell] = null;
            if (result is { } r && r.Winner == player)
                return cell;
        }
        return null;
    }
}
