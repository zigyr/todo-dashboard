namespace TodoDashboard.Models;

public sealed class ActionRecord
{
    public int Schema { get; set; } = 1;
    public string Id { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string SourceTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string NextAction { get; set; } = "";
    public string Why { get; set; } = "";
    public string Quote { get; set; } = "";
    public string Status { get; set; } = "next";
    public int Impact { get; set; } = 3;
    public int Confidence { get; set; } = 3;
    public int EffortMinutes { get; set; } = 30;
    public int Priority { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
    public string? DropReason { get; set; }
    public List<string> Tags { get; set; } = new();
    public string FilePath { get; set; } = "";
    public string Body { get; set; } = "";

    public string StatusText => Status switch
    {
        "next" => "待办",
        "doing" => "进行中",
        "done" => "已完成",
        "dropped" => "已放弃",
        _ => Status,
    };

    public string EffortText => $"{EffortMinutes} 分钟";
    public string BadgeText => $"影响 {Impact} · 信心 {Confidence} · {EffortText} · 推荐 {Priority}";
    public string SourceText => string.IsNullOrWhiteSpace(SourceTitle) ? SourceUrl : SourceTitle;

    public bool IsNext => Status == "next";
    public bool IsDoing => Status == "doing";
    public bool IsDone => Status == "done";
    public bool IsDropped => Status == "dropped";
    public bool CanDrop => IsNext || IsDoing;
    public bool HasDropReason => !string.IsNullOrWhiteSpace(DropReason);
    public string DropReasonText => HasDropReason ? $"放弃原因：{DropReason}" : "";
}
