using Microsoft.AspNetCore.Identity;

namespace SmartAnalyzer.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
    public ICollection<SavedFilter> SavedFilters { get; set; } = new List<SavedFilter>();
}
