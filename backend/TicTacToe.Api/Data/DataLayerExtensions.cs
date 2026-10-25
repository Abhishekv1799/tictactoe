using Microsoft.EntityFrameworkCore;
using TicTacToe.Api.Data.Entities;
using TicTacToe.Api.Data.Repositories;

namespace TicTacToe.Api.Data;

public static class DataLayerExtensions
{
    public const string DefaultConnectionString = "Data Source=tictactoe.db";

    public static IServiceCollection AddDataLayer(
        this IServiceCollection services, string? connectionString = null)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite(connectionString ?? DefaultConnectionString));

        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IScoreboardRepository, ScoreboardRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>
    /// Creates the SQLite file/tables if missing and seeds one scoreboard row per game mode.
    /// Call once at startup. Delete tictactoe.db to start from a clean slate.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        // One scoreboard row per game mode.
        foreach (var mode in Enum.GetValues<GameMode>())
        {
            if (!await db.Scoreboards.AnyAsync(s => s.Mode == mode))
                db.Scoreboards.Add(new Scoreboard { Mode = mode });
        }
        await db.SaveChangesAsync();
    }
}
