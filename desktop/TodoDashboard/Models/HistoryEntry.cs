namespace TodoDashboard.Models;

public sealed class HistoryEntry
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string PublishedAt { get; set; } = "";
    public int WordCount { get; set; }
    public string Summary { get; set; } = "";

    public string DateText => CreatedAt.LocalDateTime.ToString("MM月dd日");
    public string TimeText => CreatedAt.LocalDateTime.ToString("HH:mm");
    public string MetaText
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Author))
            {
                parts.Add(Author);
            }
            if (!string.IsNullOrWhiteSpace(PublishedAt))
            {
                parts.Add(PublishedAt);
            }
            if (WordCount > 0)
            {
                parts.Add($"{WordCount} 字");
            }
            return string.Join(" · ", parts);
        }
    }

    public string Snippet => Summary.Length <= 70 ? Summary : Summary[..70] + "…";
}
