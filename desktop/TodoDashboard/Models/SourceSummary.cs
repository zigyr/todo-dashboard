using TodoDashboard.Services;

namespace TodoDashboard.Models;

public sealed class SourceSummary
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Author { get; set; }
    public string FetchedAt { get; set; } = "";
    public int WordCount { get; set; }
    public string Status { get; set; } = "";
    public bool IsFirst { get; set; }

    public string DisplayMeta => $"{Formatting.ShortUrl(Url)} · {Formatting.WordCount(WordCount)}";
    public string DisplayDate => Formatting.RelativeDay(FetchedAt);
}
