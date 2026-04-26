namespace SmartAnalyzer.Models;

public class UploadedFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<DataRecord> DataRecords { get; set; } = new List<DataRecord>();
    public ICollection<FileColumn> FileColumns { get; set; } = new List<FileColumn>();
    public ICollection<SavedFilter> SavedFilters { get; set; } = new List<SavedFilter>();
}
