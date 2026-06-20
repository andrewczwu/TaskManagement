using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

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
