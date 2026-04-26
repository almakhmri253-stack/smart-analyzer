using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartAnalyzer.Data;
using SmartAnalyzer.Models;
using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Services;

public class FilterService : IFilterService
{
    private readonly ApplicationDbContext _context;

    private static readonly string[] Palette =
    [
        "#2563eb","#10b981","#f59e0b","#ef4444","#7c3aed",
        "#06b6d4","#ec4899","#84cc16","#f97316","#14b8a6","#8b5cf6","#e11d48"
    ];

    public FilterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FilterResultViewModel> ApplyFiltersAsync(FilterRequestViewModel request, string userId)
    {
        var file = await _context.UploadedFiles
            .Include(f => f.FileColumns)
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);

        if (file == null) return new FilterResultViewModel();

        var fileColumns = file.FileColumns.OrderBy(c => c.ColumnIndex).ToList();
        var columns = fileColumns.Select(c => c.ColumnName).ToList();

        var allRecords = await _context.DataRecords
            .Where(d => d.UploadedFileId == request.FileId)
            .OrderBy(d => d.RowIndex)
            .Select(d => d.JsonData)
            .ToListAsync();

        var data = allRecords
            .Select(json => JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new())
            .ToList();

        if (request.Conditions.Any(c => !string.IsNullOrEmpty(c.Column)))
            data = ApplyConditions(data, request.Conditions, request.LogicOperator);

        if (!string.IsNullOrEmpty(request.SortColumn))
        {
            var isNum = fileColumns.FirstOrDefault(c => c.ColumnName == request.SortColumn)?.DataType == "number";
            data = (request.SortAscending, isNum) switch
            {
                (true, true)  => data.OrderBy(r => ParseDouble(r, request.SortColumn)).ToList(),
                (false, true) => data.OrderByDescending(r => ParseDouble(r, request.SortColumn)).ToList(),
                (true, false) => data.OrderBy(r => r.TryGetValue(request.SortColumn, out var v) ? v : "").ToList(),
                _             => data.OrderByDescending(r => r.TryGetValue(request.SortColumn, out var v) ? v : "").ToList()
            };
        }

        var totalCount = data.Count;
        var paged = data.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

        var summary = BuildSummary(data, fileColumns);
        var chartDataSets = BuildChartDataSets(data, fileColumns);
        var chartData = BuildChartData(data, fileColumns); // keep for dashboard compat

        return new FilterResultViewModel
        {
            Data = paged,
            Columns = columns,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            Summary = summary,
            ChartData = chartData,
            ChartDataSets = chartDataSets
        };
    }

    // ── Filtering ────────────────────────────────────────────────────────────

    private static List<Dictionary<string, string>> ApplyConditions(
        List<Dictionary<string, string>> data,
        List<FilterConditionViewModel> conditions,
        string logic)
    {
        var valid = conditions.Where(c => !string.IsNullOrEmpty(c.Column)).ToList();
        if (!valid.Any()) return data;

        return data.Where(row =>
        {
            var results = valid.Select(c => EvaluateCondition(row, c)).ToList();
            return logic == "OR" ? results.Any(r => r) : results.All(r => r);
        }).ToList();
    }

    private static bool EvaluateCondition(Dictionary<string, string> row, FilterConditionViewModel cond)
    {
        if (!row.TryGetValue(cond.Column, out var rawVal)) rawVal = "";
        var op = cond.Operator.ToLower();

        if (op == "contains")
            return rawVal.Contains(cond.Value, StringComparison.OrdinalIgnoreCase);

        if (op == "between" && !string.IsNullOrEmpty(cond.Value2))
        {
            if (TryParseNumber(rawVal, out var numVal) &&
                TryParseNumber(cond.Value, out var min) &&
                TryParseNumber(cond.Value2, out var max))
                return numVal >= min && numVal <= max;

            if (DateTime.TryParse(rawVal, out var dtVal) &&
                DateTime.TryParse(cond.Value, out var dtMin) &&
                DateTime.TryParse(cond.Value2, out var dtMax))
                return dtVal >= dtMin && dtVal <= dtMax;

            return false;
        }

        if (TryParseNumber(rawVal, out var nVal) && TryParseNumber(cond.Value, out var cVal))
        {
            return op switch
            {
                "=" or "==" => nVal == cVal,
                ">"  => nVal > cVal,
                "<"  => nVal < cVal,
                ">=" => nVal >= cVal,
                "<=" => nVal <= cVal,
                "!=" => nVal != cVal,
                _    => false
            };
        }

        if (DateTime.TryParse(rawVal, out var dVal) && DateTime.TryParse(cond.Value, out var dCVal))
        {
            return op switch
            {
                "=" => dVal.Date == dCVal.Date,
                ">" => dVal > dCVal,
                "<" => dVal < dCVal,
                ">=" => dVal >= dCVal,
                "<=" => dVal <= dCVal,
                _    => false
            };
        }

        return op switch
        {
            "=" or "==" => rawVal.Equals(cond.Value, StringComparison.OrdinalIgnoreCase),
            "!="        => !rawVal.Equals(cond.Value, StringComparison.OrdinalIgnoreCase),
            "contains"  => rawVal.Contains(cond.Value, StringComparison.OrdinalIgnoreCase),
            _           => rawVal.Equals(cond.Value, StringComparison.OrdinalIgnoreCase)
        };
    }

    // ── Summary ──────────────────────────────────────────────────────────────

    private static Dictionary<string, object> BuildSummary(
        List<Dictionary<string, string>> data,
        List<FileColumn> fileColumns)
    {
        var summary = new Dictionary<string, object> { ["totalRows"] = data.Count };

        // Include explicitly numeric columns + string columns that look numeric (e.g. "$1,234.56")
        var numericCols = fileColumns.Where(c => c.DataType == "number").ToList();

        var autoDetected = fileColumns
            .Where(c => c.DataType == "string")
            .Where(col =>
            {
                var sample = data.Take(30)
                    .Select(r => r.TryGetValue(col.ColumnName, out var v) ? v : "")
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Take(15).ToList();
                return sample.Count >= 3 && sample.All(v => TryParseNumber(v, out _));
            }).ToList();

        foreach (var col in numericCols.Concat(autoDetected))
        {
            var vals = data
                .Select(r => { TryParseNumber(r.TryGetValue(col.ColumnName, out var v) ? v : "", out var d); return d; })
                .Where(v => !double.IsNaN(v))
                .ToList();

            if (!vals.Any()) continue;
            summary[$"{col.ColumnName}_sum"]   = Math.Round(vals.Sum(), 2);
            summary[$"{col.ColumnName}_avg"]   = Math.Round(vals.Average(), 2);
            summary[$"{col.ColumnName}_min"]   = Math.Round(vals.Min(), 2);
            summary[$"{col.ColumnName}_max"]   = Math.Round(vals.Max(), 2);
            summary[$"{col.ColumnName}_count"] = vals.Count;
        }

        return summary;
    }

    // ── Chart DataSets (full data, multiple charts) ───────────────────────────

    private const int ChartSampleLimit = 50_000;

    private static List<ChartDataSetViewModel> BuildChartDataSets(
        List<Dictionary<string, string>> data,
        List<FileColumn> fileColumns)
    {
        if (!data.Any()) return [];

        // Sample for very large datasets to keep chart generation fast
        if (data.Count > ChartSampleLimit)
        {
            var step = data.Count / ChartSampleLimit;
            data = data.Where((_, i) => i % step == 0).ToList();
        }

        var result = new List<ChartDataSetViewModel>();
        int idx = 0;

        var strCols = fileColumns.Where(c => c.DataType == "string").Take(3).ToList();
        var numCols = fileColumns.Where(c => c.DataType == "number").Take(4).ToList();

        // Group charts: (string col × num col)
        foreach (var strCol in strCols)
        {
            foreach (var numCol in numCols.Take(2))
            {
                var grouped = data
                    .GroupBy(r => (r.TryGetValue(strCol.ColumnName, out var v) ? v : null) ?? "(فارغ)")
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .Select(g => new
                    {
                        Key = g.Key,
                        Val = g.Sum(r =>
                        {
                            TryParseNumber(r.TryGetValue(numCol.ColumnName, out var v) ? v : "", out var d);
                            return double.IsNaN(d) ? 0 : d;
                        })
                    })
                    .OrderByDescending(x => x.Val)
                    .Take(15)
                    .ToList();

                if (grouped.Count < 2) continue;

                result.Add(new ChartDataSetViewModel
                {
                    Id = $"ac_{strCol.ColumnIndex}_{numCol.ColumnIndex}",
                    Title = $"{numCol.ColumnName} حسب {strCol.ColumnName}",
                    Labels = grouped.Select(g => g.Key).ToList(),
                    Values = grouped.Select(g => Math.Round(g.Val, 2)).ToList(),
                    ChartType = "bar",
                    Color = Palette[idx++ % Palette.Length]
                });
            }
        }

        // Distribution charts for each numeric column
        foreach (var numCol in numCols)
        {
            var vals = data
                .Select(r =>
                {
                    TryParseNumber(r.TryGetValue(numCol.ColumnName, out var v) ? v : "", out var d);
                    return (hasValue: !double.IsNaN(d), value: d);
                })
                .Where(x => x.hasValue)
                .Select(x => x.value)
                .OrderBy(v => v)
                .ToList();

            if (vals.Count < 5) continue;

            var min = vals.First();
            var max = vals.Last();
            if (max <= min) continue;

            const int buckets = 10;
            var step = (max - min) / buckets;
            var bins = new int[buckets];
            var binLabels = Enumerable.Range(0, buckets)
                .Select(k => $"{Math.Round(min + k * step, 1)}-{Math.Round(min + (k + 1) * step, 1)}")
                .ToList();

            foreach (var v in vals)
            {
                var bi = Math.Min((int)((v - min) / step), buckets - 1);
                bins[bi]++;
            }

            result.Add(new ChartDataSetViewModel
            {
                Id = $"dist_{numCol.ColumnIndex}",
                Title = $"توزيع {numCol.ColumnName}",
                Labels = binLabels,
                Values = bins.Select(b => (double)b).ToList(),
                ChartType = "bar",
                Color = Palette[idx++ % Palette.Length],
                IsDistribution = true
            });
        }

        return result;
    }

    // ── Legacy single chart (for dashboard) ──────────────────────────────────

    private static ChartDataViewModel BuildChartData(
        List<Dictionary<string, string>> data,
        List<FileColumn> fileColumns)
    {
        var firstStrCol = fileColumns.FirstOrDefault(c => c.DataType == "string");
        var firstNumCol = fileColumns.FirstOrDefault(c => c.DataType == "number");

        if (firstStrCol == null || firstNumCol == null)
            return new ChartDataViewModel { Labels = fileColumns.Select(c => c.ColumnName).ToList() };

        var grouped = data
            .GroupBy(r => r.TryGetValue(firstStrCol.ColumnName, out var v) ? v : "")
            .Take(15)
            .ToDictionary(
                g => g.Key,
                g => g.Average(r =>
                {
                    TryParseNumber(r.TryGetValue(firstNumCol.ColumnName, out var v) ? v : "", out var d);
                    return double.IsNaN(d) ? 0 : d;
                })
            );

        return new ChartDataViewModel
        {
            Labels = grouped.Keys.ToList(),
            Values = grouped.Values.Select(v => Math.Round(v, 2)).ToList(),
            ColumnName = firstNumCol.ColumnName,
            ChartType = "bar"
        };
    }

    // ── Saved Filters ─────────────────────────────────────────────────────────

    public async Task<int> SaveFilterAsync(FilterRequestViewModel request, string userId)
    {
        var saved = new SavedFilter
        {
            Name = request.SaveFilterName ?? "فلتر بلا اسم",
            FiltersJson = JsonSerializer.Serialize(request.Conditions),
            LogicOperator = request.LogicOperator,
            UserId = userId,
            UploadedFileId = request.FileId,
            CreatedAt = DateTime.UtcNow
        };
        _context.SavedFilters.Add(saved);
        await _context.SaveChangesAsync();
        return saved.Id;
    }

    public async Task<List<SavedFilterViewModel>> GetSavedFiltersAsync(int fileId, string userId)
    {
        var entities = await _context.SavedFilters
            .Where(s => s.UploadedFileId == fileId && s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return entities.Select(s => new SavedFilterViewModel
        {
            Id = s.Id,
            Name = s.Name,
            LogicOperator = s.LogicOperator,
            Conditions = JsonSerializer.Deserialize<List<FilterConditionViewModel>>(s.FiltersJson) ?? [],
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<List<EntityCardViewModel>> GetEntityCardsAsync(EntityCardsRequestViewModel request, string userId)
    {
        var file = await _context.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);
        if (file == null) return [];

        var allRecords = await _context.DataRecords
            .Where(d => d.UploadedFileId == request.FileId)
            .Select(d => d.JsonData)
            .ToListAsync();

        if (allRecords.Count == 0) return [];

        var data = allRecords
            .Select(json => JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new())
            .ToList();

        if (request.Conditions.Any(c => !string.IsNullOrEmpty(c.Column)))
            data = ApplyConditions(data, request.Conditions, request.LogicOperator);

        var total = data.Count;
        if (total == 0) return [];

        return data
            .GroupBy(r => r.TryGetValue(request.Column, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : "(فارغ)")
            .Select(g => new EntityCardViewModel
            {
                Name = g.Key,
                Count = g.Count(),
                Percentage = Math.Round(g.Count() * 100.0 / total, 1)
            })
            .OrderByDescending(e => e.Count)
            .ToList();
    }

    public async Task DeleteSavedFilterAsync(int filterId, string userId)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(s => s.Id == filterId && s.UserId == userId);
        if (filter != null)
        {
            _context.SavedFilters.Remove(filter);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<CrossTabViewModel> GetCrossTabAsync(CrossTabRequestViewModel request, string userId)
    {
        var file = await _context.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);
        if (file == null) return new CrossTabViewModel();

        var jsonRows = await _context.DataRecords
            .Where(d => d.UploadedFileId == request.FileId)
            .Select(d => d.JsonData)
            .ToListAsync();

        var data = jsonRows
            .Select(j => JsonSerializer.Deserialize<Dictionary<string, string>>(j) ?? new())
            .ToList();

        if (request.Conditions.Any(c => !string.IsNullOrEmpty(c.Column)))
            data = ApplyConditions(data, request.Conditions, request.LogicOperator);

        var col1 = request.Col1;
        var col2 = request.Col2;

        // Top values for col1 (compliance status) — keep all unique, max 10
        var col1Values = data
            .Select(r => r.TryGetValue(col1, out var v) ? v?.Trim() ?? "" : "")
            .Where(v => !string.IsNullOrEmpty(v))
            .GroupBy(v => v)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        // Top values for col2 (vendor category) — top 15 by frequency
        var col2Values = data
            .Select(r => r.TryGetValue(col2, out var v) ? v?.Trim() ?? "" : "")
            .Where(v => !string.IsNullOrEmpty(v))
            .GroupBy(v => v)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .Select(g => g.Key)
            .ToList();

        var series = col1Values.Select(c1Val => new CrossTabSeries
        {
            Name = c1Val,
            Values = col2Values.Select(c2Val =>
                (double)data.Count(r =>
                    (r.TryGetValue(col1, out var v1) ? v1?.Trim() : "") == c1Val &&
                    (r.TryGetValue(col2, out var v2) ? v2?.Trim() : "") == c2Val)
            ).ToList()
        }).ToList();

        return new CrossTabViewModel { Categories = col2Values, Series = series };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryParseNumber(string? s, out double result)
    {
        result = double.NaN;
        if (string.IsNullOrWhiteSpace(s)) return false;

        s = s.Trim();

        // plain parse
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;

        // remove thousands separators then retry  e.g. "1,234.56" → "1234.56"
        var cleaned = s.Replace(",", "");
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;

        // European decimal  e.g. "1.234,56" → "1234.56"
        cleaned = s.Replace(".", "").Replace(",", ".");
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;

        // percentage  e.g. "12.5%"
        if (s.EndsWith('%') && double.TryParse(s[..^1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            result /= 100.0;
            return true;
        }

        // currency prefix  e.g. "$1,234.56" or "USD 1234"
        var stripped = s.TrimStart('$', '€', '£', '¥', '+').Trim();
        if (stripped != s)
        {
            var c2 = stripped.Replace(",", "");
            if (double.TryParse(c2, NumberStyles.Any, CultureInfo.InvariantCulture, out result)) return true;
        }

        result = double.NaN;
        return false;
    }

    private static double ParseDouble(Dictionary<string, string> row, string col)
    {
        TryParseNumber(row.TryGetValue(col, out var v) ? v : "", out var d);
        return double.IsNaN(d) ? 0 : d;
    }
}
