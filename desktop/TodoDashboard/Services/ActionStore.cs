using System.IO;
using System.Text;
using System.Text.Json;
using TodoDashboard.Models;

namespace TodoDashboard.Services;

public sealed class ActionStore
{
    private readonly string _root;

    public ActionStore(string root)
    {
        _root = root;
        Directory.CreateDirectory(Path.Combine(root, "actions"));
    }

    public async Task<IReadOnlyList<string>> SaveAsync(
        SourcePreview source,
        IReadOnlyList<ActionItem> actions,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_root, "actions");
        Directory.CreateDirectory(directory);

        var saved = new List<string>();
        var sourceId = source.ContentHash.Length >= 12
            ? source.ContentHash[..12]
            : "source";

        foreach (var action in actions)
        {
            var id = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..24];
            action.Id = id;
            var path = Path.Combine(directory, $"{id}.md");
            var markdown = RenderMarkdown(source, sourceId, action, id);
            await AtomicWriteAsync(path, markdown, cancellationToken);
            saved.Add(path);
        }

        return saved;
    }

    private static string RenderMarkdown(
        SourcePreview source,
        string sourceId,
        ActionItem action,
        string id)
    {
        var now = DateTimeOffset.Now.ToString("O");
        var builder = new StringBuilder();
        builder.AppendLine("---");
        builder.AppendLine("schema: 1");
        builder.AppendLine($"id: {id}");
        builder.AppendLine($"source_id: {JsonSerializer.Serialize(sourceId)}");
        builder.AppendLine($"source_url: {JsonSerializer.Serialize(source.CanonicalUrl)}");
        builder.AppendLine($"source_title: {JsonSerializer.Serialize(source.Title)}");
        builder.AppendLine("status: next");
        builder.AppendLine($"impact: {action.Impact}");
        builder.AppendLine($"confidence: {action.Confidence}");
        builder.AppendLine($"effort_minutes: {action.EffortMinutes}");
        builder.AppendLine($"priority: {action.PriorityScore}");
        builder.AppendLine($"created_at: {now}");
        builder.AppendLine($"updated_at: {now}");
        builder.AppendLine("review_at: null");
        builder.AppendLine($"tags: {JsonSerializer.Serialize(action.Tags)}");
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine($"# {action.Title}");
        builder.AppendLine();
        builder.AppendLine("## 完成标准");
        builder.AppendLine(action.Outcome);
        builder.AppendLine();
        builder.AppendLine("## 下一步");
        builder.AppendLine(action.NextAction);
        builder.AppendLine();
        builder.AppendLine("## 为什么");
        builder.AppendLine(action.Why);
        builder.AppendLine();
        builder.AppendLine("## 来源");
        if (!string.IsNullOrWhiteSpace(action.Quote))
        {
            foreach (var line in action.Quote.Replace("\r\n", "\n").Split('\n'))
            {
                builder.AppendLine($"> {line}");
            }
        }
        else
        {
            builder.AppendLine($"> {source.CanonicalUrl}");
        }

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
