namespace Api.Dtos;

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsComplete,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
