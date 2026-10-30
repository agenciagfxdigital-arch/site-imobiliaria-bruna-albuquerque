using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>
/// O que o painel ou o WhatsApp informam sobre um imóvel. Quem valida é o <c>Imovel</c>.
/// <para><c>ProprietarioNome</c> e <c>Endereco</c> são dados internos (nunca vão para o site):
/// <c>null</c> = não mexer no que está gravado; vazio = apagar.</para>
/// </summary>
public sealed record DadosDoImovel
{
    /// <summary>Vazio = o sistema gera o próximo (BA101, BA102…).</summary>
    public string? Codigo { get; init; }
    public Finalidade Finalidade { get; init; }
    public TipoImovel Tipo { get; init; }
    public string Titulo { get; init; } = "";
    public string Descricao { get; init; } = "";
    public decimal? Preco { get; init; }
    public decimal? Aluguel { get; init; }
    public decimal? Condominio { get; init; }
    public decimal? Iptu { get; init; }
    public decimal? EntradaAPartir { get; init; }
    public Guid? BairroId { get; init; }
    public bool MostrarMapa { get; init; } = true;
    public int Quartos { get; init; }
    public int Suites { get; init; }
    public int Banheiros { get; init; }
    public int Vagas { get; init; }
    public decimal? Area { get; init; }
    public string Andar { get; init; } = "";
    public IReadOnlyList<string> Diferenciais { get; init; } = [];
    public bool Mcmv { get; init; }
    public bool EntradaParcelada { get; init; }
    public bool AceitaFgts { get; init; }
    public Situacao Situacao { get; init; }
    public string PrevisaoEntrega { get; init; } = "";
    public bool Mobiliado { get; init; }
    public bool AceitaPet { get; init; }
    public IReadOnlyList<string> Garantias { get; init; } = [];
    public string VideoUrl { get; init; } = "";
    public bool Destaque { get; init; }
    public StatusImovel Status { get; init; }
    public string? ProprietarioNome { get; init; }
    public string? Endereco { get; init; }

    /// <summary>Sem código informado: o caso de uso precisa pedir o próximo ao repositório.</summary>
    public bool PrecisaDeCodigoGerado => string.IsNullOrWhiteSpace(Codigo);
}
