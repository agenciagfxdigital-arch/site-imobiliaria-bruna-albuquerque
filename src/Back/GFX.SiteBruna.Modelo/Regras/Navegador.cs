using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>O que dá para saber pelo User-Agent, sem guardar o User-Agent.</summary>
public static partial class Navegador
{
    /// <summary>Robôs e pré-visualizações de link (inclusive a do WhatsApp) não contam como visita.</summary>
    public static bool EhRobo(string? userAgent) => Robo().IsMatch(userAgent ?? "");

    public static Dispositivo DispositivoDe(string? userAgent)
    {
        var ua = userAgent ?? "";
        if (Tablet().IsMatch(ua)) return Dispositivo.Tablet;
        if (Celular().IsMatch(ua)) return Dispositivo.Mobile;
        return Dispositivo.Desktop;
    }

    [GeneratedRegex("bot|crawl|spider|slurp|facebookexternalhit|whatsapp|preview|lighthouse|headless", RegexOptions.IgnoreCase)]
    private static partial Regex Robo();

    [GeneratedRegex("ipad|tablet|(android(?!.*mobile))", RegexOptions.IgnoreCase)]
    private static partial Regex Tablet();

    [GeneratedRegex("mobi|iphone|ipod|android", RegexOptions.IgnoreCase)]
    private static partial Regex Celular();
}
