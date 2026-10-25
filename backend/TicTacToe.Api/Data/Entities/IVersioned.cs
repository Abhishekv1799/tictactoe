namespace TicTacToe.Api.Data.Entities;

/// <summary>
/// Entities with an optimistic-concurrency token. <see cref="AppDbContext"/> bumps
/// <see cref="Version"/> and <see cref="UpdatedAt"/> automatically on every update.
/// </summary>
public interface IVersioned
{
    DateTime UpdatedAt { get; set; }
    int Version { get; set; }
}
