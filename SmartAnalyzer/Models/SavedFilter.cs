namespace SmartAnalyzer.Models;

public class SavedFilter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FiltersJson { get; set; } = "[]";
    public string LogicOperator { get; set; } = "AND";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int UploadedFileId { get; set; }
    public UploadedFile UploadedFile { get; set; } = null!;
}
