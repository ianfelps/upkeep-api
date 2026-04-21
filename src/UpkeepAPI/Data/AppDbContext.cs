using Microsoft.EntityFrameworkCore;
using UpkeepAPI.Models;

namespace UpkeepAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

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
