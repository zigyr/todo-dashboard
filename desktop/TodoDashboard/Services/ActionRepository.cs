using System.IO;
using System.Text;
using System.Text.Json;
using TodoDashboard.Models;

namespace TodoDashboard.Services;

public sealed class ActionRepository
{
    private readonly string _root;

    public ActionRepository(string root)
    {
        _root = root;
        Directory.CreateDirectory(Path.Combine(root, "actions"));
    }

    public string Root => _root;

    public async Task<IReadOnlyList<ActionRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_root, "actions");
        if (!Directory.Exists(directory))
        {
            return Array.Empty<ActionRecord>();
        }

        var items = new List<ActionRecord>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.md"))
        {
            var record = await ReadAsync(file, cancellationToken);
            if (record is not null)
            {
                items.Add(record);
            }
        }

        return items;
    }

    public async Task UpdateStatusAsync(
        ActionRecord action,
        string status,
        string? dropReason,
        CancellationToken cancellationToken = default)
    {
        action.Status = status;
        action.UpdatedAt = DateTimeOffset.Now.ToString("O");
        action.DropReason = status == "dropped" ? dropReason : null;
        await AtomicWriteAsync(action.FilePath, Render(action), cancellationToken);
    }

    private static async Task<ActionRecord?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        if (lines.Length < 3 || lines[0].Trim() != "---")
        {
            return null;
        }

        var end = -1;
        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].Trim() == "---")
            {
                end = index;
                break;
            }
        }

        if (end < 0)
        {
            return null;
        }

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < end; index++)
        {
            var line = lines[index];
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            fields[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        var body = string.Join("\n", lines.Skip(end + 1)).TrimStart('\n');
        var record = new ActionRecord
        {
            Schema = ReadInt(fields, "schema", 1),
            Id = ReadString(fields, "id", ""),
            SourceId = ReadString(fields, "source_id", ""),
            SourceUrl = ReadString(fields, "source_url", ""),
            SourceTitle = ReadString(fields, "source_title", ""),
            Status = ReadString(fields, "status", "next"),
            Impact = ReadInt(fields, "impact", 3),
            Confidence = ReadInt(fields, "confidence", 3),
            EffortMinutes = ReadInt(fields, "effort_minutes", 30),
            Priority = ReadInt(fields, "priority", 0),
            CreatedAt = ReadString(fields, "created_at", ""),
            UpdatedAt = ReadString(fields, "updated_at", ""),
            DropReason = ReadNullableString(fields, "drop_reason"),
            Tags = ReadTags(fields),
            FilePath = path,
            Body = body,
        };

        record.Title = ExtractTitle(body);
        record.Outcome = ExtractSection(body, "## 完成标准");
        record.NextAction = ExtractSection(body, "## 下一步");
        record.Why = ExtractSection(body, "## 为什么");
        record.Quote = string.Join(
            "\n",
            ExtractSection(body, "## 来源")
                .Split('\n')
                .Select(line => line.TrimStart('>', ' '))
                .Where(line => !string.IsNullOrWhiteSpace(line)));

        if (record.Priority <= 0)
        {
            var ease = record.EffortMinutes switch
            {
                <= 5 => 5,
                <= 15 => 4,
                <= 30 => 3,
                <= 60 => 2,
                _ => 1,
            };
            record.Priority = (int)Math.Round(record.Impact * record.Confidence * ease / 125.0 * 100);
        }

        return string.IsNullOrWhiteSpace(record.Title) ? null : record;
    }

    private static string Render(ActionRecord action)
    {
        var now = string.IsNullOrWhiteSpace(action.UpdatedAt)
            ? DateTimeOffset.Now.ToString("O")
            : action.UpdatedAt;
        var created = string.IsNullOrWhiteSpace(action.CreatedAt)
            ? now
            : action.CreatedAt;

        var builder = new StringBuilder();
        builder.AppendLine("---");
        builder.AppendLine($"schema: {action.Schema}");
        builder.AppendLine($"id: {action.Id}");
        builder.AppendLine($"source_id: {JsonSerializer.Serialize(action.SourceId)}");
        builder.AppendLine($"source_url: {JsonSerializer.Serialize(action.SourceUrl)}");
        builder.AppendLine($"source_title: {JsonSerializer.Serialize(action.SourceTitle)}");
        builder.AppendLine($"status: {action.Status}");
        builder.AppendLine($"impact: {action.Impact}");
        builder.AppendLine($"confidence: {action.Confidence}");
        builder.AppendLine($"effort_minutes: {action.EffortMinutes}");
        builder.AppendLine($"priority: {action.Priority}");
        builder.AppendLine($"created_at: {created}");
        builder.AppendLine($"updated_at: {now}");
        builder.AppendLine("review_at: null");
        builder.AppendLine($"tags: {JsonSerializer.Serialize(action.Tags)}");
        builder.AppendLine($"drop_reason: {(string.IsNullOrWhiteSpace(action.DropReason) ? "null" : JsonSerializer.Serialize(action.DropReason))}");
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine(action.Body.TrimStart('\n'));
        return builder.ToString();
    }

    private static string ReadString(Dictionary<string, string> fields, string key, string fallback)
    {
        if (!fields.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        raw = raw.Trim();
        if (raw.StartsWith('"') && raw.EndsWith('"'))
        {
            try
            {
                return JsonSerializer.Deserialize<string>(raw) ?? fallback;
            }
            catch (JsonException)
            {
                return fallback;
            }
        }

        return raw;
    }

    private static string? ReadNullableString(Dictionary<string, string> fields, string key)
    {
        if (!fields.TryGetValue(key, out var raw))
        {
            return null;
        }

        raw = raw.Trim();
        if (raw.Length == 0 || string.Equals(raw, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return ReadString(fields, key, "");
    }

    private static int ReadInt(Dictionary<string, string> fields, string key, int fallback)
    {
        return fields.TryGetValue(key, out var raw) && int.TryParse(raw, out var value)
            ? value
            : fallback;
    }

    private static List<string> ReadTags(Dictionary<string, string> fields)
    {
        if (!fields.TryGetValue("tags", out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static string ExtractTitle(string body)
    {
        foreach (var line in body.Split('\n'))
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                return line[2..].Trim();
            }
        }

        return "";
    }

    private static string ExtractSection(string body, string heading)
    {
        var lines = body.Split('\n');
        var start = -1;
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].Trim() == heading)
            {
                start = index + 1;
                break;
            }
        }

        if (start < 0)
        {
            return "";
        }

        var values = new List<string>();
        for (var index = start; index < lines.Length; index++)
        {
            if (lines[index].StartsWith("## ", StringComparison.Ordinal))
            {
                break;
            }

            values.Add(lines[index]);
        }

        return string.Join("\n", values).Trim();
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
