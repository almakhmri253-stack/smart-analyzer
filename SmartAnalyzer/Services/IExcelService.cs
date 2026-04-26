using SmartAnalyzer.Models;
using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Services;

public interface IExcelService
{
    Task<ExcelPreviewViewModel> PreviewExcelAsync(Stream stream, int sheetIndex = 0);
    Task<(List<string> columns, List<Dictionary<string, string>> rows)> ParseExcelAsync(Stream stream, int headerRow = 1, int sheetIndex = 0);
    Task<UploadedFile> SaveFileDataAsync(Stream stream, string fileName, string userId, int headerRow = 1, int sheetIndex = 0);
    Task<byte[]> ExportToExcelAsync(List<Dictionary<string, string>> data, List<string> columns);
}
