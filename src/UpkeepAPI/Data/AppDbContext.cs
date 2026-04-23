using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Models;

namespace UpkeepAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProgress> UserProgress => Set<UserProgress>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<RoutineEvent> RoutineEvents => Set<RoutineEvent>();
    public DbSet<HabitRoutineLink> HabitRoutineLinks => Set<HabitRoutineLink>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
            {
                modelBuilder.Entity(entity.ClrType, b =>
                {
                    b.HasKey("Id");
                    b.Property("CreatedAt").IsRequired();
                    b.Property("UpdatedAt").IsRequired();
                });
            }
        }

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.HasIndex(up => up.UserId).IsUnique();
            entity.HasOne(up => up.User)
                  .WithOne()
                  .HasForeignKey<UserProgress>(up => up.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Habit>(entity =>
        {
            entity.Property(h => h.Title).IsRequired().HasMaxLength(100);
            entity.Property(h => h.Color).IsRequired().HasMaxLength(7);
            entity.Property(h => h.LucideIcon).IsRequired().HasMaxLength(50);
            entity.HasOne(h => h.User)
                  .WithMany()
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HabitLog>(entity =>
        {
            entity.HasOne(hl => hl.Habit)
                  .WithMany()
                  .HasForeignKey(hl => hl.HabitId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoutineEvent>(entity =>
        {
            entity.Property(re => re.Title).IsRequired().HasMaxLength(100);
            entity.Property(re => re.DaysOfWeek).IsRequired(false);
            entity.Property(re => re.EventDate).IsRequired(false).HasColumnType("date");
            entity.Property(re => re.Color).IsRequired(false).HasMaxLength(7);
            entity.HasOne(re => re.User)
                  .WithMany()
                  .HasForeignKey(re => re.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasOne(rt => rt.User)
                  .WithMany()
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HabitRoutineLink>(entity =>
        {
            entity.HasIndex(hrl => new { hrl.HabitId, hrl.RoutineEventId }).IsUnique();
            entity.HasOne(hrl => hrl.Habit)
                  .WithMany()
                  .HasForeignKey(hrl => hrl.HabitId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(hrl => hrl.RoutineEvent)
                  .WithMany()
                  .HasForeignKey(hrl => hrl.RoutineEventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
