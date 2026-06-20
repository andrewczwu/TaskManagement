namespace Api.Models;

public class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public Guid Id { get; set; }

    // Owner; FK to ApplicationUser. Every query filters on this.
    public string UserId { get; set; } = null!;
    public ApplicationUser? User { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? DueDate { get; set; }   // UTC
    public DateTime CreatedAt { get; set; }  // UTC
    public DateTime UpdatedAt { get; set; }  // UTC
}
