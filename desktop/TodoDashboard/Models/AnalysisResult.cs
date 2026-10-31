namespace TodoDashboard.Models;

public sealed class AnalysisResult
{
    public string Summary { get; set; } = "";
    public List<KeyIdea> KeyIdeas { get; set; } = new();
    public List<ActionItem> Actions { get; set; } = new();
    public string? NoActionsReason { get; set; }
}

public sealed class KeyIdea
{
    public string Idea { get; set; } = "";
    public string WhyItMatters { get; set; } = "";
    public string Quote { get; set; } = "";
}
