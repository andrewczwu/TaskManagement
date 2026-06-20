namespace Api.Dtos;

// Title is nullable so we can return our own "required" message instead of a bind error.
public record TaskRequest(string? Title, string? Description, DateTime? DueDate, bool? IsComplete);
