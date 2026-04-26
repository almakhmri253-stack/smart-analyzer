namespace SmartAnalyzer.ViewModels;

public class AnalysisResultViewModel
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public List<ColumnInfoViewModel> Columns { get; set; } = new();
    public List<Dictionary<string, string>> PreviewData { get; set; } = new();
}

public class ColumnInfoViewModel
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = "string"; // number, string, date
    public int Index { get; set; }
}

public class ColumnStatsViewModel
{
    public string Column { get; set; } = string.Empty;
    public double Sum { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int Count { get; set; }
    public double Median { get; set; }
}

public class ComparisonRequestViewModel
{
    public int FileId { get; set; }
    public string XColumn { get; set; } = string.Empty;
    public string YColumn { get; set; } = string.Empty;
    public string ChartType { get; set; } = "bar";
    public List<FilterConditionViewModel> Conditions { get; set; } = new();
    public string LogicOperator { get; set; } = "AND";
    public string GroupBy { get; set; } = string.Empty;
    public string AggregateFunc { get; set; } = "sum"; // sum, avg, count, min, max
}

public class ComparisonResultViewModel
{
    public List<string> Labels { get; set; } = new();
    public List<double> Values { get; set; } = new();
    public List<double>? Values2 { get; set; }
    public string XColumn { get; set; } = string.Empty;
    public string YColumn { get; set; } = string.Empty;
    public int TotalFiltered { get; set; }
    public List<ColumnStatsViewModel> Stats { get; set; } = new();
}
