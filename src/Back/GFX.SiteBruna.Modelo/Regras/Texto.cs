using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Regras de texto usadas pelo domínio (URLs e comparações sem acento).</summary>
public static partial class Texto
{
    /// <summary>Minúsculas, sem acento e sem espaços extras: para comparar o que a pessoa escreveu.</summary>
    public static string Normalizar(string texto) =>
        EspacosRepetidos().Replace(SemAcentos(texto).ToLowerInvariant().Trim(), " ");

    /// <summary>Trecho de URL: sem acento, minúsculas, só letras, números e hífen, até 80 caracteres.</summary>
    public static string Slug(string texto)
    {
        var slug = ForaDoSlug().Replace(SemAcentos(texto).ToLowerInvariant(), "-").Trim('-');
        return slug.Length > 80 ? slug[..80] : slug;
    }

    private static string SemAcentos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex ForaDoSlug();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRepetidos();
}
