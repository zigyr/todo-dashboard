using System.IO;
using System.Text;
using System.Text.Json;
using TodoDashboard.Models;

namespace TodoDashboard.Services;

public sealed class SourceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _root;

    public SourceStore(string root)
    {
        _root = root;
        Workspace.Ensure(root);
    }

    public string Root => _root;

    public async Task<SavedSource> SaveAsync(SourcePreview preview, CancellationToken cancellationToken = default)
    {
        var sources = Path.Combine(_root, "sources");
        var id = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..24];
        var jsonPath = Path.Combine(sources, $"{id}.json");
        var markdownPath = Path.Combine(sources, $"{id}.md");

        var record = new SourceRecord
        {
            Schema = 1,
            Id = id,
            Url = preview.Url,
            CanonicalUrl = preview.CanonicalUrl,
            Title = preview.Title,
            Author = preview.Author,
            PublishedAt = preview.PublishedAt,
            FetchedAt = preview.FetchedAt,
            SiteName = preview.SiteName,
            Language = preview.Language,
            ContentHash = preview.ContentHash,
            WordCount = preview.WordCount,
            Excerpt = preview.Excerpt,
            ContentFile = $"{id}.md",
            Status = "captured",
        };

        await AtomicWriteAsync(
            jsonPath,
            JsonSerializer.Serialize(record, JsonOptions),
            cancellationToken);

        await AtomicWriteAsync(
            markdownPath,
            RenderMarkdown(preview),
            cancellationToken);

        return new SavedSource
        {
            Id = id,
            Title = preview.Title,
            JsonPath = jsonPath,
            MarkdownPath = markdownPath,
            SavedAt = DateTimeOffset.Now.ToString("O"),
        };
    }

    public async Task<IReadOnlyList<SourceSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var sources = Path.Combine(_root, "sources");
        if (!Directory.Exists(sources))
        {
            return Array.Empty<SourceSummary>();
        }

        var items = new List<SourceSummary>();

        foreach (var file in Directory.EnumerateFiles(sources, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var record = JsonSerializer.Deserialize<SourceRecord>(json, JsonOptions);
                if (record is null)
                {
                    continue;
                }

                items.Add(new SourceSummary
                {
                    Id = record.Id,
                    Title = record.Title,
                    Url = record.CanonicalUrl,
                    Author = record.Author,
                    FetchedAt = record.FetchedAt,
                    WordCount = record.WordCount,
                    Status = record.Status,
                });
            }
            catch (JsonException)
            {
                // A broken file should not stop the whole list.
            }
            catch (IOException)
            {
                // The file may be mid-write; it will be picked up on the next refresh.
            }
        }

        return items
            .OrderByDescending(item => item.FetchedAt, StringComparer.Ordinal)
            .ToList();
    }

    private static string RenderMarkdown(SourcePreview preview)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {preview.Title}");
        builder.AppendLine();
        builder.AppendLine($"- 来源：{preview.CanonicalUrl}");
        if (!string.IsNullOrWhiteSpace(preview.Author))
        {
            builder.AppendLine($"- 作者：{preview.Author}");
        }
        if (!string.IsNullOrWhiteSpace(preview.SiteName))
        {
            builder.AppendLine($"- 站点：{preview.SiteName}");
        }
        if (!string.IsNullOrWhiteSpace(preview.PublishedAt))
        {
            builder.AppendLine($"- 发布：{preview.PublishedAt}");
        }
        builder.AppendLine($"- 抓取：{preview.FetchedAt}");
        builder.AppendLine($"- 字数：{preview.WordCount}");
        builder.AppendLine($"- 指纹：{preview.ContentHash}");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine(preview.Text);
        return builder.ToString();
    }

    private static async Task AtomicWriteAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, content, new UTF8Encoding(false), cancellationToken);
        File.Move(temp, path, overwrite: true);
    }
}
