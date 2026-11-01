namespace TodoDashboard.Models;

public sealed class ArticleAnalysis
{
    public string Summary { get; set; } = "";
    public List<KeyIdea> KeyIdeas { get; set; } = new();
    public string Explanation { get; set; } = "";
    public string WhoItSuits { get; set; } = "";
    public string? ReadingTip { get; set; }
}

public sealed class KeyIdea
{
    public string Idea { get; set; } = "";
    public string WhyItMatters { get; set; } = "";
    public string Quote { get; set; } = "";
}
