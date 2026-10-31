namespace TodoDashboard.Models;

public sealed class SavedSource
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string JsonPath { get; set; } = "";
    public string MarkdownPath { get; set; } = "";
    public string SavedAt { get; set; } = "";
}
