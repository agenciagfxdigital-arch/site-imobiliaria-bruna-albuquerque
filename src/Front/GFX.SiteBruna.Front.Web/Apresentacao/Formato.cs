using System.Globalization;
using System.Text.RegularExpressions;
using GFX.SiteBruna.Front.Web.DTOs;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>Valores em texto, como o site em Next mostrava (<c>src/lib/format.ts</c>).</summary>
public static partial class Formato
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>"R$ 365.000" (sem centavos; espaço que não quebra, como o <c>Intl</c> do navegador).</summary>
    public static string Reais(decimal? valor) =>
        valor is { } v ? $"R$ {Math.Round(v, 0, MidpointRounding.AwayFromZero).ToString("N0", PtBr)}" : "";

    /// <summary>Preço principal do cartão e da página: "R$ 1.500/mês" no aluguel; "Consulte" sem valor.</summary>
    public static string PrecoPrincipal(Finalidade finalidade, decimal? preco, decimal? aluguel) => finalidade == Finalidade.Aluguel
        ? aluguel > 0 ? $"{Reais(aluguel)}/mês" : "Consulte"
        : preco > 0 ? Reais(preco) : "Consulte";

    /// <summary>"74,5 m²"; vazio quando não há área.</summary>
    public static string Area(decimal? valor) => valor > 0 ? $"{valor.Value.ToString("#,##0.##", PtBr)} m²" : "";

    /// <summary>"(49) 99930-4262" a partir dos dígitos (com ou sem o 55 na frente).</summary>
    public static string Telefone(string digitos)
    {
        var d = Regex.Replace(NaoDigito().Replace(digitos, ""), "^55(?=\\d{10,11}$)", "");
        return d.Length switch
        {
            11 => $"({d[..2]}) {d[2..7]}-{d[7..]}",
            10 => $"({d[..2]}) {d[2..6]}-{d[6..]}",
            _ => digitos,
        };
    }

    public static string Plural(int n, string um, string varios) => $"{n} {(n == 1 ? um : varios)}";

    /// <summary>Texto corrido de até <paramref name="maximo"/> letras, para a descrição das buscas e do compartilhamento.</summary>
    public static string Resumo(string texto, int maximo = 155)
    {
        var limpo = EspacosRepetidos().Replace(texto, " ").Trim();
        return limpo.Length > maximo ? $"{limpo[..(maximo - 1)].TrimEnd()}…" : limpo;
    }

    /// <summary>Título para o Google: "Apartamento 2 quartos à venda no bairro Centro – Videira/SC".</summary>
    public static string TituloSeo(TipoImovel tipo, int quartos, Finalidade finalidade, string? bairro, string cidade)
    {
        List<string> partes = [Rotulos.Tipo(tipo)];
        if (quartos > 0 && tipo is not (TipoImovel.Terreno or TipoImovel.SalaComercial)) partes.Add(Plural(quartos, "quarto", "quartos"));
        partes.Add(finalidade == Finalidade.Aluguel ? "para alugar" : "à venda");
        if (!string.IsNullOrWhiteSpace(bairro)) partes.Add($"no bairro {bairro}");
        return $"{string.Join(' ', partes)} – {cidade}";
    }

    /// <summary>"30/10/2026" (ou "30/10/2026 14:05") no horário de Brasília.</summary>
    public static string Data(DateTimeOffset quando, bool comHora = false)
    {
        var local = TimeZoneInfo.ConvertTime(quando, Brasilia);
        return local.ToString(comHora ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy", PtBr);
    }

    /// <summary>Parágrafos separados por linha em branco (quebras simples continuam dentro do parágrafo).</summary>
    public static IReadOnlyList<string> Paragrafos(string texto) =>
        [.. LinhaEmBranco().Split(texto ?? "").Select(p => p.Trim('\r', '\n')).Where(p => p.Trim().Length > 0)];

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigito();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRepetidos();

    [GeneratedRegex(@"(?:\r?\n){2,}")]
    private static partial Regex LinhaEmBranco();
}
