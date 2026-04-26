using System.Globalization;
using System.Text.Json;
using OfficeOpenXml;
using SmartAnalyzer.Data;
using SmartAnalyzer.Models;
using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Services;

public class ExcelService : IExcelService
{
    private readonly ApplicationDbContext _context;
    private const int ChunkSize = 2000;
    private const int PreviewRows = 50;
    private const int PreviewMaxCols = 200;

    public ExcelService(ApplicationDbContext context)
    {
        _context = context;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    // ── Preview (first N rows, no DB write) ──────────────────────────────────

    public async Task<ExcelPreviewViewModel> PreviewExcelAsync(Stream stream, int sheetIndex = 0)
    {
        try
        {
            using var package = new ExcelPackage(stream);
            var sheets = package.Workbook.Worksheets.Select(ws => ws.Name).ToList();

            if (sheets.Count == 0)
                return new ExcelPreviewViewModel { Error = "الملف لا يحتوي على صفحات." };

            sheetIndex = Math.Clamp(sheetIndex, 0, sheets.Count - 1);
            var ws = package.Workbook.Worksheets[sheetIndex];

            if (ws.Dimension == null)
                return new ExcelPreviewViewModel { Sheets = sheets, Error = "الصفحة فارغة." };

            var colCount = Math.Min(ws.Dimension.Columns, PreviewMaxCols);
            var rowCount = Math.Min(ws.Dimension.Rows, PreviewRows);

            var rows = new List<List<string>>();
            for (int r = 1; r <= rowCount; r++)
            {
                var row = Enumerable.Range(1, colCount)
                    .Select(c => ws.Cells[r, c].Text?.Trim() ?? "")
                    .ToList();
                rows.Add(row);
            }

            return await Task.FromResult(new ExcelPreviewViewModel
            {
                Sheets = sheets,
                Rows = rows,
                TotalRows = ws.Dimension.Rows,
                TotalColumns = ws.Dimension.Columns
            });
        }
        catch (Exception ex)
        {
            return new ExcelPreviewViewModel { Error = $"خطأ في قراءة الملف: {ex.Message}" };
        }
    }

    // ── Parse ─────────────────────────────────────────────────────────────────

    public async Task<(List<string> columns, List<Dictionary<string, string>> rows)> ParseExcelAsync(
        Stream stream, int headerRow = 1, int sheetIndex = 0)
    {
        var columns = new List<string>();
        var rows = new List<Dictionary<string, string>>();

        using var package = new ExcelPackage(stream);
        if (package.Workbook.Worksheets.Count == 0) return (columns, rows);

        sheetIndex = Math.Clamp(sheetIndex, 0, package.Workbook.Worksheets.Count - 1);
        var ws = package.Workbook.Worksheets[sheetIndex];

        if (ws.Dimension == null) return (columns, rows);

        headerRow = Math.Clamp(headerRow, 1, ws.Dimension.Rows);
        var colCount = ws.Dimension.Columns;
        var rowCount = ws.Dimension.Rows;

        // Build column names from header row (deduplicate)
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= colCount; col++)
        {
            var header = ws.Cells[headerRow, col].Text?.Trim();
            if (string.IsNullOrEmpty(header)) header = $"Column{col}";
            var original = header;
            int suffix = 2;
            while (!usedNames.Add(header)) header = $"{original}_{suffix++}";
            columns.Add(header);
        }

        // Data rows: everything after headerRow
        for (int row = headerRow + 1; row <= rowCount; row++)
        {
            // Skip completely empty rows
            bool hasValue = false;
            for (int col = 1; col <= colCount; col++)
            {
                if (!string.IsNullOrWhiteSpace(ws.Cells[row, col].Text)) { hasValue = true; break; }
            }
            if (!hasValue) continue;

            var rowData = new Dictionary<string, string>();
            for (int col = 1; col <= colCount; col++)
                rowData[columns[col - 1]] = ws.Cells[row, col].Text?.Trim() ?? "";
            rows.Add(rowData);
        }

        return await Task.FromResult((columns, rows));
    }

    // ── Save (chunked insert for large files) ─────────────────────────────────

    public async Task<UploadedFile> SaveFileDataAsync(
        Stream stream, string fileName, string userId, int headerRow = 1, int sheetIndex = 0)
    {
        var (columns, rows) = await ParseExcelAsync(stream, headerRow, sheetIndex);

        var uploadedFile = new UploadedFile
        {
            FileName = fileName,
            StoredFileName = $"{Guid.NewGuid()}_{fileName}",
            FileSize = stream.CanSeek ? stream.Length : 0,
            TotalRows = rows.Count,
            TotalColumns = columns.Count,
            UserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();

        // Save columns
        var fileColumns = columns.Select((col, idx) => new FileColumn
        {
            UploadedFileId = uploadedFile.Id,
            ColumnName = col,
            ColumnIndex = idx,
            DataType = DetectColumnType(rows, col)
        }).ToList();

        _context.FileColumns.AddRange(fileColumns);
        await _context.SaveChangesAsync();

        // Chunked insert for large files
        for (int i = 0; i < rows.Count; i += ChunkSize)
        {
            var chunk = rows.Skip(i).Take(ChunkSize)
                .Select((row, localIdx) => new DataRecord
                {
                    UploadedFileId = uploadedFile.Id,
                    RowIndex = i + localIdx + 1,
                    JsonData = JsonSerializer.Serialize(row)
                }).ToList();

            _context.DataRecords.AddRange(chunk);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear(); // prevent memory buildup
        }

        return uploadedFile;
    }

    // ── Export ────────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportToExcelAsync(List<Dictionary<string, string>> data, List<string> columns)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Results");

        for (int i = 0; i < columns.Count; i++)
        {
            var cell = ws.Cells[1, i + 1];
            cell.Value = columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 78, 121));
            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        for (int r = 0; r < data.Count; r++)
            for (int c = 0; c < columns.Count; c++)
                ws.Cells[r + 2, c + 1].Value = data[r].TryGetValue(columns[c], out var v) ? v : "";

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        return await Task.FromResult(package.GetAsByteArray());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string DetectColumnType(List<Dictionary<string, string>> rows, string col)
    {
        var sample = rows
            .Take(200)
            .Select(r => r.TryGetValue(col, out var v) ? v?.Trim() : "")
            .Where(v => !string.IsNullOrEmpty(v))
            .Take(50)
            .ToList();

        if (sample.Count == 0) return "string";
        if (sample.All(v => TryParseNumber(v!, out _))) return "number";
        if (sample.All(v => DateTime.TryParse(v, out _))) return "date";
        return "string";
    }

    private static bool TryParseNumber(string s, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();

        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;

        var cleaned = s.Replace(",", "");
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;

        cleaned = s.Replace(".", "").Replace(",", ".");
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;

        if (s.EndsWith('%') && double.TryParse(s[..^1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            result /= 100.0;
            return true;
        }

        // currency prefix  e.g. "$1,234.56"
        var stripped = s.TrimStart('$', '€', '£', '¥', '+').Trim();
        if (stripped != s)
        {
            var c2 = stripped.Replace(",", "");
            if (double.TryParse(c2, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;
        }

        return false;
    }
}
