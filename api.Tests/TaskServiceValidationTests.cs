using Api.Dtos;
using Api.Models;
using Api.Services;

namespace Api.Tests;

public class TaskServiceValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_MissingTitle_FailsValidation(string? title)
    {
        // Arrange
        using var ctx = new TestContext();
        var userId = await ctx.SeedUserAsync("alice@x.com");
        var service = new TaskService(ctx.Db);

        // Act
        var result = await service.CreateAsync(userId, new TaskRequest(title, null, null, null));

        // Assert
        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        Assert.Contains("Title", result.Errors!.Keys);
    }

    [Fact]
    public async Task Create_TitleTooLong_FailsValidation()
    {
        // Arrange
        using var ctx = new TestContext();
        var userId = await ctx.SeedUserAsync("alice@x.com");
        var service = new TaskService(ctx.Db);
        var longTitle = new string('a', TaskItem.TitleMaxLength + 1);

        // Act
        var result = await service.CreateAsync(userId, new TaskRequest(longTitle, null, null, null));

        // Assert
        Assert.Equal(ResultStatus.ValidationFailed, result.Status);
        Assert.Contains("Title", result.Errors!.Keys);
    }

    [Fact]
    public async Task Create_ValidTitle_Succeeds()
    {
        // Arrange
        using var ctx = new TestContext();
        var userId = await ctx.SeedUserAsync("alice@x.com");
        var service = new TaskService(ctx.Db);

        // Act
        var result = await service.CreateAsync(userId, new TaskRequest("  Buy milk  ", null, null, null));

        // Assert
        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal("Buy milk", result.Value!.Title); // trimmed
    }
}
