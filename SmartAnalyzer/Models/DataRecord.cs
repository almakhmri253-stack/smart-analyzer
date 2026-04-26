namespace SmartAnalyzer.Models;

public class DataRecord
{
    public int Id { get; set; }
    public int UploadedFileId { get; set; }
    public UploadedFile UploadedFile { get; set; } = null!;
    public int RowIndex { get; set; }
    public string JsonData { get; set; } = "{}"; // stored as JSON key-value
}
