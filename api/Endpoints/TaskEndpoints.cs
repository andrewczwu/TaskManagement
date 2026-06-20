using System.Security.Claims;
using Api.Dtos;
using Api.Services;

namespace Api.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var tasks = routes.MapGroup("/tasks").RequireAuthorization();

        tasks.MapGet("", async (TaskService service, ClaimsPrincipal user) =>
            Results.Ok(await service.GetAllAsync(user.GetUserId())));

        tasks.MapGet("/{id:guid}", async (Guid id, TaskService service, ClaimsPrincipal user) =>
        {
            var task = await service.GetByIdAsync(user.GetUserId(), id);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        tasks.MapPost("", async (TaskRequest req, TaskService service, ClaimsPrincipal user) =>
        {
            var result = await service.CreateAsync(user.GetUserId(), req);
            return result.Status == ResultStatus.ValidationFailed
                ? Results.ValidationProblem(result.Errors!)
                : Results.Created($"/api/v1/tasks/{result.Value!.Id}", result.Value);
        });

        tasks.MapPut("/{id:guid}", async (Guid id, TaskRequest req, TaskService service, ClaimsPrincipal user) =>
        {
            var result = await service.UpdateAsync(user.GetUserId(), id, req);
            return result.Status switch
            {
                ResultStatus.ValidationFailed => Results.ValidationProblem(result.Errors!),
                ResultStatus.NotFound => Results.NotFound(),
                _ => Results.Ok(result.Value),
            };
        });

        tasks.MapDelete("/{id:guid}", async (Guid id, TaskService service, ClaimsPrincipal user) =>
            await service.DeleteAsync(user.GetUserId(), id) ? Results.NoContent() : Results.NotFound());
    }

    // [Authorize] guarantees the principal carries the Identity user id.
    private static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated principal has no user id.");
}
