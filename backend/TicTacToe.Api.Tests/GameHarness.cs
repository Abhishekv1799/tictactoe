using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Data;
using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Data.Repositories;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Tests;

/// <summary>
/// Runs the real services and repositories against an in-memory SQLite database.
/// Every call uses a fresh DI scope, like separate HTTP requests, so each test also
/// proves that state is actually persisted between requests.
/// </summary>
public sealed class GameHarness : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;

    public GameHarness()
    {
        _connection.Open();   // the in-memory database lives as long as this connection

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<GameService>();
        services.AddScoped<ScoreboardService>();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        foreach (var mode in Enum.GetValues<GameMode>())
            db.Scoreboards.Add(new Scoreboard { Mode = mode });
        db.SaveChanges();
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    private async Task<T> InScope<T>(Func<GameService, Task<T>> action)
    {
        using var scope = _provider.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<GameService>());
    }

    public Task<GameResponse> CreateAsync(GameMode mode) =>
        InScope(s => s.CreateAsync(mode, CancellationToken.None));

    public Task<GameResponse> GetAsync(Guid id) =>
        InScope(s => s.GetAsync(id, CancellationToken.None));

    /// <summary>Plays <paramref name="player"/> at a 0-8 cell index.</summary>
    public Task<GameResponse> MoveAsync(Guid id, Player player, int cell) =>
        InScope(s => s.MakeMoveAsync(id, new MoveRequest(player, cell / 3 + 1, cell % 3 + 1), CancellationToken.None));

    public Task<GameResponse> UndoAsync(Guid id) =>
        InScope(s => s.UndoAsync(id, CancellationToken.None));

    public Task<GameResponse> ResetAsync(Guid id) =>
        InScope(s => s.ResetAsync(id, CancellationToken.None));

    public async Task<ScoreboardDto> ScoreAsync(GameMode mode = GameMode.TwoPlayer)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ScoreboardService>().GetAsync(mode, CancellationToken.None);
    }

    public async Task<ScoreboardDto> ResetScoreAsync(GameMode mode = GameMode.TwoPlayer)
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ScoreboardService>().ResetAsync(mode, CancellationToken.None);
    }

    /// <summary>Plays alternating moves, X first, in a two-player game.</summary>
    public async Task<GameResponse> PlayAsync(Guid id, params int[] cells)
    {
        GameResponse? last = null;
        for (var i = 0; i < cells.Length; i++)
            last = await MoveAsync(id, i % 2 == 0 ? Player.X : Player.O, cells[i]);
        return last ?? await GetAsync(id);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
