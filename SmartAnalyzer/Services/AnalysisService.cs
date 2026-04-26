using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartAnalyzer.Data;
using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Services;

public class AnalysisService : IAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly IFilterService _filterService;

    public AnalysisService(ApplicationDbContext context, IFilterService filterService)
    {
        _context = context;
        _filterService = filterService;
    }

    public async Task<AnalysisResultViewModel> GetFileAnalysisAsync(int fileId, string userId)
    {
        var file = await _context.UploadedFiles
            .Include(f => f.FileColumns)
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

        if (file == null) return new AnalysisResultViewModel();

        var previewJson = await _context.DataRecords
            .Where(d => d.UploadedFileId == fileId)
            .OrderBy(d => d.RowIndex)
            .Take(8)
            .Select(d => d.JsonData)
            .ToListAsync();

        return new AnalysisResultViewModel
        {
            FileId = fileId,
            FileName = file.FileName,
            TotalRows = file.TotalRows,
            TotalColumns = file.TotalColumns,
            Columns = file.FileColumns.OrderBy(c => c.ColumnIndex).Select(c => new ColumnInfoViewModel
            {
                Name = c.ColumnName,
                DataType = c.DataType,
                Index = c.ColumnIndex
            }).ToList(),
            PreviewData = previewJson
                .Select(j => JsonSerializer.Deserialize<Dictionary<string, string>>(j) ?? new())
                .ToList()
        };
    }

    public async Task<ComparisonResultViewModel> GetComparisonAsync(ComparisonRequestViewModel request, string userId)
    {
        var filterRequest = new FilterRequestViewModel
        {
            FileId = request.FileId,
            Conditions = request.Conditions,
            LogicOperator = request.LogicOperator,
            Page = 1,
            PageSize = int.MaxValue
        };

        var filtered = await _filterService.ApplyFiltersAsync(filterRequest, userId);
        var data = filtered.Data;

        var result = new ComparisonResultViewModel
        {
            XColumn = request.XColumn,
            YColumn = request.YColumn,
            TotalFiltered = data.Count
        };

        // Build stats for all numeric columns
        var numericCols = filtered.Columns.Where(col =>
        {
            var sample = data.Take(10).Select(r => r.TryGetValue(col, out var v) ? v : "").Where(v => !string.IsNullOrEmpty(v)).ToList();
            return sample.Any() && sample.All(v => double.TryParse(v, out _));
        }).ToList();

        result.Stats = numericCols.Select(col =>
        {
            var vals = data
                .Select(r => r.TryGetValue(col, out var v) && double.TryParse(v, out var d) ? d : (double?)null)
                .Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();

            if (!vals.Any()) return null;

            return new ColumnStatsViewModel
            {
                Column = col,
                Sum = Math.Round(vals.Sum(), 2),
                Average = Math.Round(vals.Average(), 2),
                Min = vals.Min(),
                Max = vals.Max(),
                Count = vals.Count,
                Median = vals.Count % 2 == 0
                    ? Math.Round((vals[vals.Count / 2 - 1] + vals[vals.Count / 2]) / 2.0, 2)
                    : Math.Round(vals[vals.Count / 2], 2)
            };
        }).Where(s => s != null).Select(s => s!).ToList();

        // Build chart data
        if (!string.IsNullOrEmpty(request.XColumn) && !string.IsNullOrEmpty(request.YColumn))
        {
            var grouped = data
                .GroupBy(r => r.TryGetValue(request.XColumn, out var v) ? v : "")
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .OrderByDescending(g => g.Count())
                .Take(20)
                .ToList();

            result.Labels = grouped.Select(g => g.Key).ToList();
            result.Values = grouped.Select(g =>
            {
                if (request.AggregateFunc == "count")
                    return (double)g.Count();

                var vals = g.Select(r => r.TryGetValue(request.YColumn, out var v) && double.TryParse(v.Replace(",","").Replace("$","").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : (double?)null)
                             .Where(v => v.HasValue).Select(v => v!.Value).ToList();

                if (!vals.Any()) return 0.0;

                return Math.Round(request.AggregateFunc switch
                {
                    "avg" => vals.Average(),
                    "min" => vals.Min(),
                    "max" => vals.Max(),
                    _ => vals.Sum()
                }, 2);
            }).ToList();
        }

        return result;
    }
}
