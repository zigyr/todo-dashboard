using System;
using System.Windows;

namespace TodoDashboard.Services;

/// <summary>
/// 只做一件事：把原文链接放进剪贴板。
/// 这个 Windows 会话不允许应用拉起或驱动浏览器，所以不做任何跳转尝试。
/// </summary>
public static class LinkClipboard
{
    public const string CopiedMessage = "链接已复制，粘贴到浏览器地址栏即可打开。";
    public const string CopyFailedMessage = "复制失败，请手动选中链接后复制。";
    public const string NoLinkMessage = "这条记录没有可用的原文链接。";

    public static bool TryCopy(string? rawUrl)
    {
        var url = Normalize(rawUrl);
        if (url is null)
        {
            return false;
        }

        try
        {
            Clipboard.SetText(url);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string? Normalize(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return null;
        }

        return Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    public static string CopyAndDescribe(string? rawUrl)
    {
        if (Normalize(rawUrl) is null)
        {
            return NoLinkMessage;
        }

        return TryCopy(rawUrl) ? CopiedMessage : CopyFailedMessage;
    }
}
