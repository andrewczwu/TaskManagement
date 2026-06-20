using System.Text.Json;
using Api.Data;
using Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests;

// Guards the UTC value converter: SQLite drops DateTimeKind, so without it
// DateTimes read back as Unspecified and serialize without a 'Z' (timezone bug).
public class UtcRoundTripTest
{
    [Fact]
    public async Task DateTimes_RoundTrip_AsUtc()
    {
        // Arrange
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        await using (var seed = new AppDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1@x.com" });
            seed.Tasks.Add(new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                Title = "t",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            });
            await seed.SaveChangesAsync();
        }

        // Act
        await using var ctx = new AppDbContext(options);
        var task = await ctx.Tasks.SingleAsync();

        // Assert
        Assert.Equal(DateTimeKind.Utc, task.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, task.DueDate!.Value.Kind);
        Assert.EndsWith("Z", JsonSerializer.Serialize(task.CreatedAt).Trim('"'));
    }
}
