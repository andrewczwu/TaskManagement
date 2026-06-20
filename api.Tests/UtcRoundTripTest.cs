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
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;

        await using (var ctx = new AppDbContext(options))
        {
            await ctx.Database.EnsureCreatedAsync();
            ctx.Users.Add(new ApplicationUser { Id = "u1", UserName = "u1@x.com" });
            ctx.Tasks.Add(new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                Title = "t",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new AppDbContext(options))
        {
            var task = await ctx.Tasks.SingleAsync();
            Assert.Equal(DateTimeKind.Utc, task.CreatedAt.Kind);
            Assert.Equal(DateTimeKind.Utc, task.DueDate!.Value.Kind);
            Assert.EndsWith("Z", JsonSerializer.Serialize(task.CreatedAt).Trim('"'));
        }
    }
}
