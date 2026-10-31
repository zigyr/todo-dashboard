namespace TodoDashboard.Models;

public sealed class SourcePreview
{
    public string Url { get; set; } = "";
    public string CanonicalUrl { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Author { get; set; }
    public string? PublishedAt { get; set; }
    public string FetchedAt { get; set; } = "";
    public string? SiteName { get; set; }
    public string Text { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public int WordCount { get; set; }
    public string Language { get; set; } = "";
    public string ContentHash { get; set; } = "";
}
