using System.Globalization;
using System.Text.RegularExpressions;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>Números digitados no painel: aceita "250.000", "250000" e "1.250,50" (como o formulário do site em Next).</summary>
public static partial class Numero
{
    public static decimal? Ler(string? texto)
    {
        var limpo = ForaDoNumero().Replace(texto ?? "", "");
        if (limpo.Length == 0) return null;
        var normalizado = limpo.Contains(',')
            ? limpo.Replace(".", "").Replace(',', '.')
            : MilharComPonto().Replace(limpo, "");
        return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    public static string Escrever(decimal? valor) => valor?.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR")) ?? "";

    /// <summary>Contagem (quartos, vagas...) entre 0 e 99.</summary>
    public static int Contagem(string? texto) => Math.Clamp(int.TryParse(texto, out var n) ? n : 0, 0, 99);

    [GeneratedRegex(@"[^\d,.-]")]
    private static partial Regex ForaDoNumero();

    [GeneratedRegex(@"\.(?=\d{3}(\D|$))")]
    private static partial Regex MilharComPonto();
}
