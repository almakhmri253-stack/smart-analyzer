namespace SmartAnalyzer.ViewModels;

public class BotCommandRequest
{
    public int FileId { get; set; }
    public string Message { get; set; } = "";
    public string State { get; set; } = "idle";
    public Dictionary<string, string>? StateData { get; set; }
}

public class BotCommandResult
{
    public string Reply { get; set; } = "";
    public string NewState { get; set; } = "idle";
    public Dictionary<string, string>? StateData { get; set; }
    public bool Success { get; set; }
    public string ActionType { get; set; } = "info";
    public int AffectedRows { get; set; }
    public bool RefreshData { get; set; }
    public BotTableData? TableData { get; set; }
}

public class BotTableData
{
    public string Title { get; set; } = "";
    public List<string> Columns { get; set; } = [];
    public List<BotTableRow> Rows { get; set; } = [];
}

public class BotTableRow
{
    public int RowIndex { get; set; }
    public Dictionary<string, string> Data { get; set; } = [];
}
