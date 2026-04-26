using System.ComponentModel.DataAnnotations;

namespace SmartAnalyzer.ViewModels;

public class FilterConditionViewModel
{
    public string Column { get; set; } = string.Empty;
    public string Operator { get; set; } = "=";
    public string Value { get; set; } = string.Empty;
    public string? Value2 { get; set; } // for "between"
}

public class FilterRequestViewModel
{
    public int FileId { get; set; }
    public List<FilterConditionViewModel> Conditions { get; set; } = new();
    public string LogicOperator { get; set; } = "AND"; // AND / OR
    public string? SaveFilterName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortColumn { get; set; }
    public bool SortAscending { get; set; } = true;
}

public class FilterResultViewModel
{
    public List<Dictionary<string, string>> Data { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public Dictionary<string, object> Summary { get; set; } = new();
    public ChartDataViewModel ChartData { get; set; } = new();
    public List<ChartDataSetViewModel> ChartDataSets { get; set; } = new();
}

public class ChartDataSetViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = new();
    public List<double> Values { get; set; } = new();
    public string ChartType { get; set; } = "bar";
    public string Color { get; set; } = "#2563eb";
    public bool IsDistribution { get; set; }
}

public class ChartDataViewModel
{
    public List<string> Labels { get; set; } = new();
    public List<double> Values { get; set; } = new();
    public string ChartType { get; set; } = "bar";
    public string ColumnName { get; set; } = string.Empty;
}

public class SavedFilterViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogicOperator { get; set; } = "AND";
    public List<FilterConditionViewModel> Conditions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class EntityCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class EntityCardsRequestViewModel
{
    public int FileId { get; set; }
    public string Column { get; set; } = string.Empty;
    public List<FilterConditionViewModel> Conditions { get; set; } = new();
    public string LogicOperator { get; set; } = "AND";
}

public class CrossTabRequestViewModel
{
    public int FileId { get; set; }
    public string Col1 { get; set; } = string.Empty;
    public string Col2 { get; set; } = string.Empty;
    public List<FilterConditionViewModel> Conditions { get; set; } = new();
    public string LogicOperator { get; set; } = "AND";
}

public class CrossTabViewModel
{
    public List<string> Categories { get; set; } = new(); // col2 values (X-axis)
    public List<CrossTabSeries> Series { get; set; } = new();  // one per col1 value
}

public class CrossTabSeries
{
    public string Name { get; set; } = string.Empty;
    public List<double> Values { get; set; } = new();
}
