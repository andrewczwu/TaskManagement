using Api.Data;
using Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests;

// A real SQLite database held in memory (so FK/relational constraints apply),
// kept alive by one open connection for the lifetime of the test.
public sealed class TestContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Db { get; }

    public TestContext()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
    }

    // Tasks have an FK to AspNetUsers, so owners must exist before adding tasks.
    public async Task<string> SeedUserAsync(string email)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = email, Email = email };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user.Id;
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
