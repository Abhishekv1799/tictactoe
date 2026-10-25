namespace TicTacToe.Api.Services;

/// <summary>An expected, client-caused failure that maps to an HTTP error response.</summary>
public sealed class GameException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;

    public static GameException NotFound(Guid id) =>
        new(StatusCodes.Status404NotFound, "Game not found", $"No game with id {id}.");

    public static GameException BadRequest(string message) =>
        new(StatusCodes.Status400BadRequest, "Invalid request", message);

    public static GameException Conflict(string message) =>
        new(StatusCodes.Status409Conflict, "Move not allowed", message);
}
