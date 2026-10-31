using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using TodoDashboard.Models;

namespace TodoDashboard.Services;

public sealed class CaptureException : Exception
{
    public CaptureException(string message) : base(message)
    {
    }
}

public sealed class UrlCaptureService
{
    private const int MaxBytes = 5 * 1024 * 1024;

    private static readonly HttpClient Client = CreateClient();

    public async Task<SourcePreview> CaptureAsync(string rawUrl, CancellationToken cancellationToken = default)
    {
        var normalized = UrlNormalizer.Normalize(rawUrl)
            ?? throw new CaptureException("网址格式不正确。请输入以 http:// 或 https:// 开头的网址。");

        var current = new Uri(normalized);

        for (var hop = 0; hop <= 5; hop++)
        {
            await EnsurePublicAsync(current, cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");

            using var response = await Client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if ((int)response.StatusCode is >= 300 and < 400)
            {
                var location = response.Headers.Location
                    ?? throw new CaptureException("重定向缺少目标地址。");
                current = new Uri(current, location);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new CaptureException($"服务器返回 {(int)response.StatusCode}。");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrWhiteSpace(mediaType)
                && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                throw new CaptureException($"这不是网页（{mediaType}）。");
            }

            if (response.Content.Headers.ContentLength is > MaxBytes)
            {
                throw new CaptureException("页面超过 5 MB，暂不处理。");
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (html.Length > MaxBytes)
            {
                throw new CaptureException("页面超过 5 MB，暂不处理。");
            }

            return Extract(html, current);
        }

        throw new CaptureException("重定向次数过多。");
    }

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "TodoDashboard/0.1 (+https://github.com/zigyr/todo-dashboard)");
        return client;
    }

    private static async Task EnsurePublicAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new CaptureException("只支持 http 或 https 网址。");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new CaptureException("网址里不能包含用户名或密码。");
        }

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new CaptureException("这个网址指向本机，出于安全考虑不能抓取。");
        }

        if (IPAddress.TryParse(host, out var literal))
        {
            if (IsBlocked(literal))
            {
                throw new CaptureException("这个网址指向本机或内网，出于安全考虑不能抓取。");
            }
            return;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        }
        catch (Exception)
        {
            throw new CaptureException("域名解析失败，请检查网址或网络。");
        }

        if (addresses.Length == 0)
        {
            throw new CaptureException("这个域名没有可用地址。");
        }

        foreach (var address in addresses)
        {
            if (IsBlocked(address))
            {
                throw new CaptureException("这个网址指向本机或内网，出于安全考虑不能抓取。");
            }
        }
    }

    private static bool IsBlocked(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 0
                || bytes[0] == 10
                || bytes[0] == 127
                || (bytes[0] == 169 && bytes[1] == 254)
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || bytes[0] >= 224;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return address.Equals(IPAddress.IPv6Any)
                || address.Equals(IPAddress.IPv6Loopback)
                || address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || (bytes[0] & 0xFE) == 0xFC;
        }

        return true;
    }

    private static SourcePreview Extract(string html, Uri uri)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        foreach (var node in document.QuerySelectorAll("script, style, noscript, nav, header, footer, aside, form, svg"))
        {
            node.Remove();
        }

        var title = Clean(
            Meta(document, "meta[property='og:title']")
            ?? Meta(document, "meta[name='twitter:title']")
            ?? Text(document, "title")
            ?? "未命名文章");

        var author = CleanOrNull(
            Meta(document, "meta[name='author']")
            ?? Meta(document, "meta[property='article:author']")
            ?? Text(document, "[rel='author']"));

        var publishedAt = CleanOrNull(
            Meta(document, "meta[property='article:published_time']")
            ?? Attr(document, "time[datetime]", "datetime"));

        var siteName = CleanOrNull(Meta(document, "meta[property='og:site_name']"));
        var text = ExtractBody(document);
        var canonical = new UriBuilder(uri) { Fragment = "" }.Uri.ToString();

        return new SourcePreview
        {
            Url = uri.ToString(),
            CanonicalUrl = canonical,
            Title = string.IsNullOrWhiteSpace(title) ? "未命名文章" : title,
            Author = author,
            PublishedAt = publishedAt,
            FetchedAt = DateTimeOffset.Now.ToString("O"),
            SiteName = siteName,
            Text = text,
            Excerpt = MakeExcerpt(text, 280),
            WordCount = CountWords(text),
            Language = DetectLanguage(text),
            ContentHash = Hash(text),
        };
    }

    private static string ExtractBody(IDocument document)
    {
        var selectors = new[]
        {
            "article",
            "main",
            "[role='main']",
            ".post-content",
            ".entry-content",
            ".article-content",
            ".markdown-body",
            "body",
        };

        var best = "";

        foreach (var selector in selectors)
        {
            var element = document.QuerySelector(selector);
            if (element is null)
            {
                continue;
            }

            var text = Normalize(element.TextContent);
            if (text.Length >= 200)
            {
                return text;
            }

            if (text.Length > best.Length)
            {
                best = text;
            }
        }

        if (best.Length < 120)
        {
            throw new CaptureException("这个页面没有可提取的正文。");
        }

        return best;
    }

    private static string? Meta(IDocument document, string selector)
    {
        return document.QuerySelector(selector)?.GetAttribute("content")?.Trim();
    }

    private static string? Attr(IDocument document, string selector, string attribute)
    {
        return document.QuerySelector(selector)?.GetAttribute(attribute)?.Trim();
    }

    private static string? Text(IDocument document, string selector)
    {
        return document.QuerySelector(selector)?.TextContent?.Trim();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : Regex.Replace(value, "\\s+", " ").Trim();
    }

    private static string? CleanOrNull(string? value)
    {
        var cleaned = Clean(value);
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var builder = new StringBuilder();
        var blankRun = 0;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.Trim().Length == 0)
            {
                blankRun++;
                if (blankRun > 1)
                {
                    continue;
                }
            }
            else
            {
                blankRun = 0;
            }

            builder.AppendLine(line);
        }

        return builder.ToString().Trim();
    }

    private static string MakeExcerpt(string text, int maxChars)
    {
        return text.Length <= maxChars ? text : text[..maxChars] + "…";
    }

    private static int CountWords(string text)
    {
        var cjk = text.Count(IsCjk);
        var rest = new string(text.Select(ch => IsCjk(ch) ? ' ' : ch).ToArray());
        return cjk + rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string DetectLanguage(string text)
    {
        var cjk = text.Count(IsCjk);
        var latinWords = text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Count(word => word.Any(ch => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z'));

        if (cjk > 0 && cjk >= latinWords * 2)
        {
            return "zh";
        }
        return latinWords > 0 ? "en" : "unknown";
    }

    private static bool IsCjk(char ch)
    {
        return ch is >= '\u3400' and <= '\u4DBF'
            or >= '\u4E00' and <= '\u9FFF'
            or >= '\u3040' and <= '\u30FF'
            or >= '\uAC00' and <= '\uD7AF';
    }

    private static string Hash(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
}
