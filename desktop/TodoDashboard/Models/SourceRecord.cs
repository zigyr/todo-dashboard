namespace TodoDashboard.Models;

public sealed class SourceRecord
{
    public int Schema { get; set; } = 1;
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string CanonicalUrl { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Author { get; set; }
    public string? PublishedAt { get; set; }
    public string FetchedAt { get; set; } = "";
    public string? SiteName { get; set; }
    public string Language { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public int WordCount { get; set; }
    public string Excerpt { get; set; } = "";
    public string ContentFile { get; set; } = "";
    public string Status { get; set; } = "captured";
}
