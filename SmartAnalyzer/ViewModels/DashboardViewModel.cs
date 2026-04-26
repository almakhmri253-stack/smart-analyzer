using SmartAnalyzer.Models;

namespace SmartAnalyzer.ViewModels;

public class ExcelPreviewViewModel
{
    public List<string> Sheets { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public string? Error { get; set; }
}

public class DashboardViewModel
{
    public List<UploadedFile> UserFiles { get; set; } = new();
    public UploadedFile? SelectedFile { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, string>> PreviewData { get; set; } = new();
    public int TotalRows { get; set; }
    public List<SavedFilterViewModel> SavedFilters { get; set; } = new();
    public int TotalFiles { get; set; }
    public long TotalRecords { get; set; }
}
