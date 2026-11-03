using System.Globalization;
using GFX.SiteBruna.Front.Web.DTOs;
using Microsoft.AspNetCore.Http;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>
/// Filtros da lista de imóveis como aparecem na URL, com os mesmos nomes do site em Next (<c>?tipo=casa&amp;quartos=2</c>):
/// links filtrados já compartilhados e usados em anúncios continuam valendo. A Api confere tudo de novo.
/// </summary>
public sealed record FiltroDoSite
{
    public Finalidade Finalidade { get; init; }
    public TipoImovel? Tipo { get; init; }
    /// <summary>Slug do bairro.</summary>
    public string? Bairro { get; init; }
    public decimal? PrecoMinimo { get; init; }
    public decimal? PrecoMaximo { get; init; }
    public int? Quartos { get; init; }
    public int? Vagas { get; init; }
    public Situacao? Situacao { get; init; }
    public bool Mcmv { get; init; }
    public bool EntradaParcelada { get; init; }
    public bool AceitaFgts { get; init; }
    public bool Mobiliado { get; init; }
    public bool AceitaPet { get; init; }
    public OrdemDoSite Ordem { get; init; }
    public int Pagina { get; init; } = 1;

    private static readonly Dictionary<string, TipoImovel> TiposNaUrl = new()
    {
        ["apartamento"] = TipoImovel.Apartamento,
        ["casa"] = TipoImovel.Casa,
        ["terreno"] = TipoImovel.Terreno,
        ["sala_comercial"] = TipoImovel.SalaComercial,
    };

    private static readonly Dictionary<string, OrdemDoSite> OrdensNaUrl = new()
    {
        ["recentes"] = OrdemDoSite.Recentes,
        ["menor"] = OrdemDoSite.MenorPreco,
        ["maior"] = OrdemDoSite.MaiorPreco,
    };

    public static string NaUrl(TipoImovel tipo) => TiposNaUrl.First(t => t.Value == tipo).Key;

    public static string NaUrl(OrdemDoSite ordem) => OrdensNaUrl.First(o => o.Value == ordem).Key;

    public static string NaUrl(Situacao situacao) => situacao == DTOs.Situacao.NaPlanta ? "na_planta" : "pronto";

    /// <summary>
    /// Lê a URL. Sem JavaScript, o formulário manda a faixa de preço pela posição (<c>faixa=2</c>) e campos vazios: a página
    /// redireciona para o endereço limpo (<see cref="PrecisaLimparAUrl"/>).
    /// </summary>
    public static FiltroDoSite DaUrl(Finalidade finalidade, IQueryCollection query)
    {
        string? Valor(string nome) => query[nome].FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        decimal? Numero(string nome) =>
            decimal.TryParse(Valor(nome), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) && n > 0 ? n : null;
        int? Inteiro(string nome) => int.TryParse(Valor(nome), out var n) && n > 0 ? n : null;
        bool Marcado(string nome) => Valor(nome) == "1";

        var venda = finalidade == Finalidade.Venda;
        var (minimo, maximo) = (Numero("preco_min"), Numero("preco_max"));
        if (minimo is null && maximo is null && int.TryParse(Valor("faixa"), out var faixa) && faixa >= 0
            && faixa < FaixaDePreco.Da(finalidade).Count)
            (minimo, maximo) = (FaixaDePreco.Da(finalidade)[faixa].Minimo, FaixaDePreco.Da(finalidade)[faixa].Maximo);

        return new FiltroDoSite
        {
            Finalidade = finalidade,
            Tipo = Valor("tipo") is { } tipo && TiposNaUrl.TryGetValue(tipo, out var t) ? t : null,
            Bairro = Valor("bairro") is { } bairro ? bairro[..Math.Min(bairro.Length, 80)] : null,
            PrecoMinimo = minimo,
            PrecoMaximo = maximo,
            Quartos = Inteiro("quartos"),
            Vagas = Inteiro("vagas"),
            Situacao = Valor("situacao") switch
            {
                "pronto" => DTOs.Situacao.Pronto,
                "na_planta" => DTOs.Situacao.NaPlanta,
                _ => null,
            },
            Mcmv = venda && Marcado("mcmv"),
            EntradaParcelada = venda && Marcado("entrada"),
            AceitaFgts = venda && Marcado("fgts"),
            Mobiliado = !venda && Marcado("mobiliado"),
            AceitaPet = !venda && Marcado("pet"),
            Ordem = Valor("ordem") is { } ordem && OrdensNaUrl.TryGetValue(ordem, out var o) ? o : OrdemDoSite.Recentes,
            Pagina = Inteiro("pagina") ?? 1,
        };
    }

    /// <summary>A URL veio do formulário sem JavaScript (faixa pela posição ou campos vazios).</summary>
    public static bool PrecisaLimparAUrl(IQueryCollection query) =>
        query.ContainsKey("faixa") || query.Any(q => q.Value.All(string.IsNullOrWhiteSpace));

    /// <summary>"?tipo=casa&amp;quartos=2" (vazio sem filtros), na mesma ordem do site em Next.</summary>
    public string ParaUrl()
    {
        List<string> partes = [];
        void Incluir(string nome, string? valor)
        {
            if (!string.IsNullOrEmpty(valor)) partes.Add($"{nome}={Uri.EscapeDataString(valor)}");
        }
        static string? Num(decimal? n) => n?.ToString("0.##", CultureInfo.InvariantCulture);

        Incluir("tipo", Tipo is { } tipo ? NaUrl(tipo) : null);
        Incluir("bairro", Bairro);
        Incluir("preco_min", Num(PrecoMinimo));
        Incluir("preco_max", Num(PrecoMaximo));
        Incluir("quartos", Quartos?.ToString(CultureInfo.InvariantCulture));
        Incluir("vagas", Vagas?.ToString(CultureInfo.InvariantCulture));
        Incluir("situacao", Situacao is { } situacao ? NaUrl(situacao) : null);
        Incluir("mcmv", Mcmv ? "1" : null);
        Incluir("entrada", EntradaParcelada ? "1" : null);
        Incluir("fgts", AceitaFgts ? "1" : null);
        Incluir("mobiliado", Mobiliado ? "1" : null);
        Incluir("pet", AceitaPet ? "1" : null);
        Incluir("ordem", Ordem != OrdemDoSite.Recentes ? NaUrl(Ordem) : null);
        Incluir("pagina", Pagina > 1 ? Pagina.ToString(CultureInfo.InvariantCulture) : null);
        return partes.Count > 0 ? "?" + string.Join('&', partes) : "";
    }

    /// <summary>Quantos filtros estão em uso (o botão "Filtrar (3)" do celular).</summary>
    public int Quantidade =>
        new[]
        {
            Tipo is not null, Bairro is not null, PrecoMinimo is not null || PrecoMaximo is not null, Quartos is not null,
            Vagas is not null, Situacao is not null, Mcmv, EntradaParcelada, AceitaFgts, Mobiliado, AceitaPet,
        }.Count(f => f);
}
