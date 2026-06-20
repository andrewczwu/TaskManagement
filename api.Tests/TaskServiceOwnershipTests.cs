using Api.Dtos;
using Api.Services;

namespace Api.Tests;

// User A must never reach User B's data.
public class TaskServiceOwnershipTests
{
    private static TaskRequest NewTask(string title = "Task") => new(title, null, null, null);

    [Fact]
    public async Task GetById_OtherUsersTask_ReturnsNull()
    {
        // Arrange
        using var ctx = new TestContext();
        var alice = await ctx.SeedUserAsync("alice@x.com");
        var bob = await ctx.SeedUserAsync("bob@x.com");
        var service = new TaskService(ctx.Db);
        var created = await service.CreateAsync(alice, NewTask());

        // Act
        var result = await service.GetByIdAsync(bob, created.Value!.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Update_OtherUsersTask_ReturnsNotFound()
    {
        // Arrange
        using var ctx = new TestContext();
        var alice = await ctx.SeedUserAsync("alice@x.com");
        var bob = await ctx.SeedUserAsync("bob@x.com");
        var service = new TaskService(ctx.Db);
        var created = await service.CreateAsync(alice, NewTask());

        // Act
        var result = await service.UpdateAsync(bob, created.Value!.Id, NewTask("Hacked"));

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Delete_OtherUsersTask_ReturnsFalse()
    {
        // Arrange
        using var ctx = new TestContext();
        var alice = await ctx.SeedUserAsync("alice@x.com");
        var bob = await ctx.SeedUserAsync("bob@x.com");
        var service = new TaskService(ctx.Db);
        var created = await service.CreateAsync(alice, NewTask());

        // Act
        var deleted = await service.DeleteAsync(bob, created.Value!.Id);

        // Assert
        Assert.False(deleted);
        Assert.NotNull(await service.GetByIdAsync(alice, created.Value!.Id)); // still there for owner
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyCallersTasks()
    {
        // Arrange
        using var ctx = new TestContext();
        var alice = await ctx.SeedUserAsync("alice@x.com");
        var bob = await ctx.SeedUserAsync("bob@x.com");
        var service = new TaskService(ctx.Db);
        await service.CreateAsync(alice, NewTask("Alice 1"));
        await service.CreateAsync(alice, NewTask("Alice 2"));
        await service.CreateAsync(bob, NewTask("Bob 1"));

        // Act
        var aliceTasks = await service.GetAllAsync(alice);

        // Assert
        Assert.Equal(2, aliceTasks.Count);
        Assert.All(aliceTasks, t => Assert.StartsWith("Alice", t.Title));
    }
}
