using System.Globalization;
using System.IO;
using System.Text;
using TodoDashboard.Models;

namespace TodoDashboard.Services;

public sealed class HistoryStore
{
    private static readonly string[] Header =
    {
        "created_at", "url", "title", "author", "published_at", "word_count", "summary",
    };

    public HistoryStore(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public static HistoryStore Resolve()
    {
        var custom = Environment.GetEnvironmentVariable("TODO_DASHBOARD_HISTORY");
        if (!string.IsNullOrWhiteSpace(custom))
        {
            return new HistoryStore(custom);
        }

        var local = Path.Combine(AppContext.BaseDirectory, "data", "history.csv");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(local)!);
            return new HistoryStore(local);
        }
        catch
        {
            var fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TodoDashboard",
                "history.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(fallback)!);
            return new HistoryStore(fallback);
        }
    }

    public Task<List<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return LoadFromAsync(FilePath, cancellationToken);
    }

    public async Task<List<HistoryEntry>> LoadFromAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return new List<HistoryEntry>();
        }

        var text = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        return Parse(text);
    }

    public async Task SaveAllAsync(IEnumerable<HistoryEntry> entries, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", Header));
        foreach (var entry in entries)
        {
            builder.AppendLine(string.Join(",", new[]
            {
                Escape(entry.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                Escape(entry.Url),
                Escape(entry.Title),
                Escape(entry.Author),
                Escape(entry.PublishedAt),
                Escape(entry.WordCount.ToString(CultureInfo.InvariantCulture)),
                Escape(entry.Summary),
            }));
        }

        var temp = FilePath + ".tmp";
        await File.WriteAllTextAsync(temp, builder.ToString(), new UTF8Encoding(true), cancellationToken);
        File.Move(temp, FilePath, overwrite: true);
    }

    private static List<HistoryEntry> Parse(string text)
    {
        var rows = ParseRows(text);
        if (rows.Count == 0)
        {
            return new List<HistoryEntry>();
        }

        var start = 0;
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (rows[0].Any(value => string.Equals(value.Trim(), Header[0], StringComparison.OrdinalIgnoreCase)))
        {
            for (var index = 0; index < rows[0].Count; index++)
            {
                indexes[rows[0][index].Trim()] = index;
            }
            start = 1;
        }
        else
        {
            for (var index = 0; index < Header.Length; index++)
            {
                indexes[Header[index]] = index;
            }
        }

        var entries = new List<HistoryEntry>();
        for (var rowIndex = start; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var createdAt = Get(row, indexes, "created_at");
            if (!DateTimeOffset.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var created))
            {
                created = DateTimeOffset.Now;
            }

            var entry = new HistoryEntry
            {
                CreatedAt = created,
                Url = Get(row, indexes, "url"),
                Title = Get(row, indexes, "title"),
                Author = Get(row, indexes, "author"),
                PublishedAt = Get(row, indexes, "published_at"),
                WordCount = int.TryParse(Get(row, indexes, "word_count"), out var words) ? words : 0,
                Summary = Get(row, indexes, "summary"),
            };

            if (!string.IsNullOrWhiteSpace(entry.Url) || !string.IsNullOrWhiteSpace(entry.Title))
            {
                entries.Add(entry);
            }
        }

        return entries.OrderByDescending(entry => entry.CreatedAt).ToList();
    }

    private static string Get(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> indexes, string name)
    {
        return indexes.TryGetValue(name, out var index) && index < row.Count ? row[index] : "";
    }

    private static List<List<string>> ParseRows(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (quoted)
            {
                if (current == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(current);
                }
                continue;
            }

            switch (current)
            {
                case '"':
                    quoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    if (index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }
                    AddRow(rows, row, field);
                    break;
                case '\n':
                    AddRow(rows, row, field);
                    break;
                default:
                    field.Append(current);
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            AddRow(rows, row, field);
        }

        return rows;
    }

    private static void AddRow(List<List<string>> rows, List<string> row, StringBuilder field)
    {
        row.Add(field.ToString());
        field.Clear();
        if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            rows.Add(row.ToList());
        }
        row.Clear();
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
