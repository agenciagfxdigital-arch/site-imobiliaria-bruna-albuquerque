using System.Text.RegularExpressions;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>Links de contato: WhatsApp (o objetivo principal do site) e redes sociais.</summary>
public static partial class Contato
{
    /// <summary>Link do wa.me com a mensagem pronta. Número com 10 ou 11 dígitos ganha o 55 do Brasil.</summary>
    public static string Whatsapp(string numero, string? mensagem)
    {
        var digitos = NaoDigito().Replace(numero ?? "", "");
        if (digitos.Length is 10 or 11) digitos = $"55{digitos}";
        return string.IsNullOrEmpty(mensagem) ? $"https://wa.me/{digitos}" : $"https://wa.me/{digitos}?text={Uri.EscapeDataString(mensagem)}";
    }

    public static string MensagemDoImovel(string codigo, string titulo) => $"Olá, Bruna! Tenho interesse no imóvel {codigo} – {titulo}.";

    /// <summary>Aceita "@bruna", "bruna" ou o endereço completo.</summary>
    public static string Instagram(string perfil)
    {
        if (string.IsNullOrWhiteSpace(perfil)) return "";
        return perfil.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? perfil : $"https://instagram.com/{perfil.TrimStart('@')}";
    }

    /// <summary>"@bruna", a partir do que estiver salvo nas configurações.</summary>
    public static string ArrobaDoInstagram(string perfil) =>
        "@" + PrefixoDoInstagram().Replace(perfil.Trim(), "").TrimEnd('/');

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigito();

    [GeneratedRegex(@"^@|.*instagram\.com/", RegexOptions.IgnoreCase)]
    private static partial Regex PrefixoDoInstagram();
}
