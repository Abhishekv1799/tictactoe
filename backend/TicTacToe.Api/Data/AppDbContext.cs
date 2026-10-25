using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TicTacToe.Api.Data.Entities;

namespace TicTacToe.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Move> Moves => Set<Move>();
    public DbSet<Scoreboard> Scoreboards => Set<Scoreboard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // int[] <-> "0,4,8" so the winning line fits in one TEXT column.
        var cellsConverter = new ValueConverter<int[]?, string>(
            v => string.Join(',', v!),
            v => v.Length == 0
                ? Array.Empty<int>()
                : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray());

        var cellsComparer = new ValueComparer<int[]>(
            (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
            v => v.Aggregate(0, (h, x) => h * 31 + x),
            v => v.ToArray());

        modelBuilder.Entity<Game>(e =>
        {
            e.ToTable("Games", t =>
            {
                t.HasCheckConstraint("CK_Games_Mode", "Mode IN ('TwoPlayer','VsComputer')");
                t.HasCheckConstraint("CK_Games_Status", "Status IN ('InProgress','Won','Draw')");
                t.HasCheckConstraint("CK_Games_CurrentPlayer", "CurrentPlayer IN ('X','O')");
                // Winner and winning line exist if and only if the game is Won.
                t.HasCheckConstraint("CK_Games_WinnerMatchesStatus",
                    "(Status = 'Won' AND Winner IS NOT NULL AND WinningCells IS NOT NULL) " +
                    "OR (Status <> 'Won' AND Winner IS NULL AND WinningCells IS NULL)");
            });

            e.HasKey(g => g.Id);
            e.Property(g => g.Id).ValueGeneratedNever();
            e.Property(g => g.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(g => g.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            e.Property(g => g.CurrentPlayer).HasConversion<string>().HasMaxLength(1).IsRequired();
            e.Property(g => g.Winner).HasConversion<string>().HasMaxLength(1);
            e.Property(g => g.RecordedOutcome).HasConversion<string>().HasMaxLength(8);
            e.Property(g => g.WinningCells)
                .HasConversion(cellsConverter, cellsComparer)
                .HasMaxLength(16);
            e.Property(g => g.Version).IsConcurrencyToken();

            e.HasMany(g => g.Moves)
                .WithOne(m => m.Game)
                .HasForeignKey(m => m.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Move>(e =>
        {
            e.ToTable("Moves", t =>
            {
                t.HasCheckConstraint("CK_Moves_CellIndex", "CellIndex BETWEEN 0 AND 8");
                t.HasCheckConstraint("CK_Moves_MoveNumber", "MoveNumber BETWEEN 1 AND 9");
                t.HasCheckConstraint("CK_Moves_Player", "Player IN ('X','O')");
            });

            e.HasKey(m => m.Id);
            e.Property(m => m.Player).HasConversion<string>().HasMaxLength(1).IsRequired();
            e.Ignore(m => m.Row);
            e.Ignore(m => m.Column);

            // A cell can be played once per game, and move numbers can't repeat.
            e.HasIndex(m => new { m.GameId, m.MoveNumber }).IsUnique();
            e.HasIndex(m => new { m.GameId, m.CellIndex }).IsUnique();
        });

        modelBuilder.Entity<Scoreboard>(e =>
        {
            e.ToTable("Scoreboard", t =>
            {
                t.HasCheckConstraint("CK_Scoreboard_Mode", "Mode IN ('TwoPlayer','VsComputer')");
                t.HasCheckConstraint("CK_Scoreboard_NonNegative",
                    "XWins >= 0 AND OWins >= 0 AND Draws >= 0");
            });

            // One row per game mode, keyed by the mode itself.
            e.HasKey(s => s.Mode);
            e.Property(s => s.Mode).HasConversion<string>().HasMaxLength(16).ValueGeneratedNever();
            e.Property(s => s.Version).IsConcurrencyToken();
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Touch();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        Touch();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Bump the concurrency token and timestamp on every modified versioned entity.</summary>
    private void Touch()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IVersioned>()
                     .Where(x => x.State == EntityState.Modified))
        {
            entry.Entity.Version++;
            entry.Entity.UpdatedAt = now;
        }
    }
}
