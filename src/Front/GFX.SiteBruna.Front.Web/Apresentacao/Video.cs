using System.Text.RegularExpressions;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>Links do YouTube e do Instagram (Reels/post) viram endereço de incorporação.</summary>
public static partial class Video
{
    public static string? Incorporar(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var u)) return null;
        var host = Regex.Replace(u.Host, "^(www\\.|m\\.)", "");
        var caminho = u.AbsolutePath;
        if (host == "youtu.be") return $"https://www.youtube-nocookie.com/embed/{caminho.TrimStart('/')}";
        if (host == "youtube.com")
        {
            if (caminho.StartsWith("/shorts/")) return $"https://www.youtube-nocookie.com/embed/{caminho.Split('/')[2]}";
            if (caminho.StartsWith("/embed/")) return $"https://www.youtube-nocookie.com{caminho}";
            var v = System.Web.HttpUtility.ParseQueryString(u.Query)["v"];
            if (!string.IsNullOrEmpty(v)) return $"https://www.youtube-nocookie.com/embed/{Uri.EscapeDataString(v)}";
        }
        if (host == "instagram.com" && PostDoInstagram().Match(caminho) is { Success: true } m)
            return $"https://www.instagram.com/{(m.Groups[1].Value == "reels" ? "reel" : m.Groups[1].Value)}/{m.Groups[2].Value}/embed";
        return null;
    }

    /// <summary>Reels e Shorts são em pé (9:16).</summary>
    public static bool EmPe(string url) => url.Contains("instagram.com") || url.Contains("/shorts/");

    [GeneratedRegex("^/(reel|reels|p|tv)/([^/]+)")]
    private static partial Regex PostDoInstagram();
}
