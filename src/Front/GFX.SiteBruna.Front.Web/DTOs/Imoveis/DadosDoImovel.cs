namespace GFX.SiteBruna.Front.Web.DTOs.Imoveis;

/// <summary>
/// O formulário do imóvel no painel (mutável, ligado aos campos). Quem valida é o <c>Imovel</c> do Modelo, na Api.
/// <c>ProprietarioNome</c> e <c>Endereco</c> são dados internos: nunca aparecem no site.
/// </summary>
public sealed class DadosDoImovel
{
    /// <summary>Vazio = a Api gera o próximo (BA101, BA102…).</summary>
    public string? Codigo { get; set; }
    public Finalidade Finalidade { get; set; }
    public TipoImovel Tipo { get; set; }
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public decimal? Preco { get; set; }
    public decimal? Aluguel { get; set; }
    public decimal? Condominio { get; set; }
    public decimal? Iptu { get; set; }
    public decimal? EntradaAPartir { get; set; }
    public Guid? BairroId { get; set; }
    public bool MostrarMapa { get; set; } = true;
    public int Quartos { get; set; }
    public int Suites { get; set; }
    public int Banheiros { get; set; }
    public int Vagas { get; set; }
    public decimal? Area { get; set; }
    public string Andar { get; set; } = "";
    public List<string> Diferenciais { get; set; } = [];
    public bool Mcmv { get; set; }
    public bool EntradaParcelada { get; set; }
    public bool AceitaFgts { get; set; }
    public Situacao Situacao { get; set; }
    public string PrevisaoEntrega { get; set; } = "";
    public bool Mobiliado { get; set; }
    public bool AceitaPet { get; set; }
    public List<string> Garantias { get; set; } = [];
    public string VideoUrl { get; set; } = "";
    public bool Destaque { get; set; }
    public StatusImovel Status { get; set; }
    public string? ProprietarioNome { get; set; }
    public string? Endereco { get; set; }
}
