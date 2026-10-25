namespace TicTacToe.Api.Data.Repositories;

/// <summary>
/// Commits everything the repositories changed in one atomic transaction, so a game
/// and the scoreboard can never be saved out of step.
/// </summary>
public interface IUnitOfWork
{
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException">
    /// The game or scoreboard changed since it was loaded (map to HTTP 409).
    /// </exception>
    Task<int> CommitAsync(CancellationToken ct = default);
}

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
