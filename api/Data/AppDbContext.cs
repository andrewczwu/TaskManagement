using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    // SQLite drops DateTimeKind, so DateTimes read back as Unspecified. Force UTC
    // on read (and normalize on write) so they serialize with a 'Z' suffix.
    private static readonly ValueConverter<DateTime, DateTime> Utc = new(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullable = new(
        v => v.HasValue ? v.Value.ToUniversalTime() : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TaskItem>(task =>
        {
            task.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(TaskItem.TitleMaxLength);

            task.Property(t => t.Description)
                .HasMaxLength(TaskItem.DescriptionMaxLength);

            task.Property(t => t.UserId).IsRequired();

            task.Property(t => t.CreatedAt).HasConversion(Utc);
            task.Property(t => t.UpdatedAt).HasConversion(Utc);
            task.Property(t => t.DueDate).HasConversion(UtcNullable);

            // One user has many tasks; deleting a user removes their tasks (no orphans).
            task.HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Every task query filters by owner.
            task.HasIndex(t => t.UserId);
        });
    }
}
