using System.Globalization;
using GFX.SiteBruna.Front.Web.DTOs;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>Faixas de preço dos filtros (as mesmas do site em Next, para os links de anúncios continuarem iguais).</summary>
public sealed record FaixaDePreco(string Rotulo, decimal? Minimo, decimal? Maximo)
{
    public static readonly IReadOnlyList<FaixaDePreco> Venda =
    [
        new("Até R$ 200 mil", null, 200_000),
        new("R$ 200 a 300 mil", 200_000, 300_000),
        new("R$ 300 a 450 mil", 300_000, 450_000),
        new("R$ 450 a 700 mil", 450_000, 700_000),
        new("Acima de R$ 700 mil", 700_000, null),
    ];

    public static readonly IReadOnlyList<FaixaDePreco> Aluguel =
    [
        new("Até R$ 1.200", null, 1_200),
        new("R$ 1.200 a 1.800", 1_200, 1_800),
        new("R$ 1.800 a 2.500", 1_800, 2_500),
        new("Acima de R$ 2.500", 2_500, null),
    ];

    public static IReadOnlyList<FaixaDePreco> Da(Finalidade finalidade) => finalidade == Finalidade.Venda ? Venda : Aluguel;

    /// <summary>Posição da faixa escolhida (-1 = qualquer valor ou valores que não batem com nenhuma faixa).</summary>
    public static int Indice(Finalidade finalidade, decimal? minimo, decimal? maximo)
    {
        var faixas = Da(finalidade);
        for (var i = 0; i < faixas.Count; i++)
            if (faixas[i].Minimo == minimo && faixas[i].Maximo == maximo) return i;
        return -1;
    }

    /// <summary>Nome da faixa para as métricas ("R$ 200 a 300 mil", "Até 1.500"...).</summary>
    public static string? Descrever(Finalidade finalidade, decimal? minimo, decimal? maximo)
    {
        if (minimo is null && maximo is null) return null;
        var indice = Indice(finalidade, minimo, maximo);
        if (indice >= 0) return Da(finalidade)[indice].Rotulo;
        static string Numero(decimal n) => n.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"));
        if (minimo is { } min && maximo is { } max) return $"{Numero(min)} a {Numero(max)}";
        return minimo is { } so ? $"Acima de {Numero(so)}" : $"Até {Numero(maximo!.Value)}";
    }
}
