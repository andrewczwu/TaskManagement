using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

// All operations are scoped to userId; a task that isn't the caller's is "not found".
public class TaskService(AppDbContext db)
{
    public async Task<List<TaskResponse>> GetAllAsync(string userId)
    {
        var tasks = await db.Tasks
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.IsComplete)        // incomplete first
            .ThenBy(t => t.DueDate == null)    // tasks with a due date first
            .ThenBy(t => t.DueDate)            // soonest due first
            .ThenByDescending(t => t.CreatedAt) // then newest
            .ToListAsync();
        return tasks.Select(ToResponse).ToList();
    }

    public async Task<TaskResponse?> GetByIdAsync(string userId, Guid id)
    {
        var task = await Find(userId, id);
        return task is null ? null : ToResponse(task);
    }

    public async Task<ServiceResult<TaskResponse>> CreateAsync(string userId, TaskRequest req)
    {
        var errors = Validate(req);
        if (errors.Count > 0) return ServiceResult<TaskResponse>.Invalid(errors);

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = req.Title!.Trim(),
            Description = req.Description,
            DueDate = req.DueDate,
            IsComplete = req.IsComplete ?? false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<ServiceResult<TaskResponse>> UpdateAsync(string userId, Guid id, TaskRequest req)
    {
        var errors = Validate(req);
        if (errors.Count > 0) return ServiceResult<TaskResponse>.Invalid(errors);

        var task = await Find(userId, id);
        if (task is null) return ServiceResult<TaskResponse>.NotFound();

        task.Title = req.Title!.Trim();
        task.Description = req.Description;
        task.DueDate = req.DueDate;
        task.IsComplete = req.IsComplete ?? task.IsComplete;
        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<bool> DeleteAsync(string userId, Guid id)
    {
        var task = await Find(userId, id);
        if (task is null) return false;
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return true;
    }

    // Ownership boundary: only ever match the caller's task.
    private Task<TaskItem?> Find(string userId, Guid id) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

    private static Dictionary<string, string[]> Validate(TaskRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        var title = req.Title?.Trim();

        if (string.IsNullOrEmpty(title))
            errors[nameof(req.Title)] = ["Title is required."];
        else if (title.Length > TaskItem.TitleMaxLength)
            errors[nameof(req.Title)] = [$"Title must be {TaskItem.TitleMaxLength} characters or fewer."];

        if (req.Description?.Length > TaskItem.DescriptionMaxLength)
            errors[nameof(req.Description)] = [$"Description must be {TaskItem.DescriptionMaxLength} characters or fewer."];

        return errors;
    }

    private static TaskResponse ToResponse(TaskItem t) =>
        new(t.Id, t.Title, t.Description, t.IsComplete, t.DueDate, t.CreatedAt, t.UpdatedAt);
}
