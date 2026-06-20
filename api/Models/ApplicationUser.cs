using Microsoft.AspNetCore.Identity;

namespace Api.Models;

// Identity user; Id (string GUID), Email, PasswordHash, etc. come from IdentityUser.
public class ApplicationUser : IdentityUser
{
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
