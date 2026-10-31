using System.Globalization;

namespace TodoDashboard.Services;

public static class Formatting
{
    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "时间未知";
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed.LocalDateTime.ToString("yyyy年M月d日 HH:mm", CultureInfo.InvariantCulture)
            : value;
    }

    public static string RelativeDay(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return "未知";
        }

        var days = (DateTimeOffset.Now.Date - parsed.LocalDateTime.Date).Days;
        return days switch
        {
            <= 0 => "今天",
            1 => "昨天",
            _ => parsed.LocalDateTime.ToString("M月d日", CultureInfo.InvariantCulture),
        };
    }

    public static string WordCount(int count)
    {
        if (count >= 10000)
        {
            return $"{count / 10000.0:0.0} 万字";
        }
        if (count >= 1000)
        {
            return $"{count / 1000.0:0.0} 千字";
        }
        return $"{count} 字";
    }

    public static string ShortUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? $"{uri.Host}{uri.AbsolutePath}"
            : value;
    }
}
