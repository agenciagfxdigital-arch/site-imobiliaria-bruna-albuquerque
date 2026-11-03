using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>
/// Filtros da lista de imóveis do site. Vêm da URL (o link filtrado pode ser compartilhado e usado em anúncios), então
/// qualquer valor pode chegar: <see cref="Normalizado"/> descarta o que não faz sentido em vez de recusar.
/// </summary>
public sealed record FiltroDoSite
{
    public const int ItensPorPagina = 12;
    public const int UltimaPagina = 1000;

    public Finalidade Finalidade { get; init; }
    public TipoImovel? Tipo { get; init; }
    /// <summary>Slug do bairro (ex.: <c>santa-gema</c>).</summary>
    public string? Bairro { get; init; }
    /// <summary>Preço de venda ou valor do aluguel, conforme a finalidade.</summary>
    public decimal? PrecoMinimo { get; init; }
    public decimal? PrecoMaximo { get; init; }
    /// <summary>Pelo menos este número de quartos.</summary>
    public int? Quartos { get; init; }
    /// <summary>Pelo menos este número de vagas.</summary>
    public int? Vagas { get; init; }
    public Situacao? Situacao { get; init; }
    public bool Mcmv { get; init; }
    public bool EntradaParcelada { get; init; }
    public bool AceitaFgts { get; init; }
    public bool Mobiliado { get; init; }
    public bool AceitaPet { get; init; }
    public OrdemDoSite Ordem { get; init; }
    public int Pagina { get; init; } = 1;

    /// <summary>Quantos imóveis pular para chegar à <see cref="Pagina"/>.</summary>
    public int Pular => (Math.Clamp(Pagina, 1, UltimaPagina) - 1) * ItensPorPagina;

    /// <summary>
    /// Sem o que não vale para a finalidade (MCMV, entrada e FGTS só na venda; mobiliado e pet só no aluguel), sem
    /// valores zerados ou negativos e com a página entre 1 e <see cref="UltimaPagina"/>.
    /// </summary>
    public FiltroDoSite Normalizado()
    {
        var venda = Finalidade == Finalidade.Venda;
        var bairro = Texto.Slug(Bairro ?? "");
        return this with
        {
            Finalidade = venda ? Finalidade.Venda : Finalidade.Aluguel,
            Tipo = Tipo is { } tipo && Enum.IsDefined(tipo) ? tipo : null,
            Bairro = bairro.Length > 0 ? bairro : null,
            PrecoMinimo = PrecoMinimo > 0 ? PrecoMinimo : null,
            PrecoMaximo = PrecoMaximo > 0 ? PrecoMaximo : null,
            Quartos = Quartos is > 0 and < 100 ? Quartos : null,
            Vagas = Vagas is > 0 and < 100 ? Vagas : null,
            Situacao = Situacao is { } situacao && Enum.IsDefined(situacao) ? situacao : null,
            Mcmv = venda && Mcmv,
            EntradaParcelada = venda && EntradaParcelada,
            AceitaFgts = venda && AceitaFgts,
            Mobiliado = !venda && Mobiliado,
            AceitaPet = !venda && AceitaPet,
            Ordem = Enum.IsDefined(Ordem) ? Ordem : OrdemDoSite.Recentes,
            Pagina = Math.Clamp(Pagina, 1, UltimaPagina),
        };
    }
}
