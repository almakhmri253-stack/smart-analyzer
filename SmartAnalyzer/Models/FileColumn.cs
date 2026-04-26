namespace SmartAnalyzer.Models;

public class FileColumn
{
    public int Id { get; set; }
    public int UploadedFileId { get; set; }
    public UploadedFile UploadedFile { get; set; } = null!;
    public string ColumnName { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }
    public string DataType { get; set; } = "string"; // string, number, date
}
