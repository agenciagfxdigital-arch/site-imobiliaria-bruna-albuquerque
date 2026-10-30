using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>
/// Campos informados pela ficha do WhatsApp: <c>null</c> = não informado (mantém o que está gravado).
/// Status, destaque e código ficam de fora: status e destaque só mudam por ação confirmada, e o código não muda
/// pelo WhatsApp (links já divulgados quebrariam).
/// </summary>
public sealed record CamposDoImovel
{
    public Finalidade? Finalidade { get; init; }
    public TipoImovel? Tipo { get; init; }
    public string? Titulo { get; init; }
    public string? Descricao { get; init; }
    public decimal? Preco { get; init; }
    public decimal? Aluguel { get; init; }
    public decimal? Condominio { get; init; }
    public decimal? Iptu { get; init; }
    public decimal? EntradaAPartir { get; init; }
    public Guid? BairroId { get; init; }
    public bool? MostrarMapa { get; init; }
    public int? Quartos { get; init; }
    public int? Suites { get; init; }
    public int? Banheiros { get; init; }
    public int? Vagas { get; init; }
    public decimal? Area { get; init; }
    public string? Andar { get; init; }
    public IReadOnlyList<string>? Diferenciais { get; init; }
    public bool? Mcmv { get; init; }
    public bool? EntradaParcelada { get; init; }
    public bool? AceitaFgts { get; init; }
    public Situacao? Situacao { get; init; }
    public string? PrevisaoEntrega { get; init; }
    public bool? Mobiliado { get; init; }
    public bool? AceitaPet { get; init; }
    public IReadOnlyList<string>? Garantias { get; init; }
    public string? VideoUrl { get; init; }
    public string? ProprietarioNome { get; init; }
    public string? Endereco { get; init; }

    public bool Vazio => this == new CamposDoImovel();

    /// <summary>Confere o que veio antes de propor: precisa de algum campo e garantias só da lista oficial.</summary>
    public void Validar()
    {
        if (Vazio) throw DomainException.Validacao("Nenhum campo para alterar.");
        if (Garantias?.Any(g => !Opcoes.Garantias.Contains(g)) == true)
            throw DomainException.Validacao($"Garantias aceitas: {string.Join(", ", Opcoes.Garantias)}.");
    }

    /// <summary>
    /// Rascunho completo a partir do que a conversa já tem; o resto recebe valores neutros. Sempre nasce como rascunho,
    /// fora dos destaques e sem código (o sistema gera).
    /// </summary>
    public DadosDoImovel ParaNovoRascunho()
    {
        if (Finalidade is not { } finalidade) throw DomainException.Validacao("finalidade: use venda ou aluguel.");
        if (Tipo is not { } tipo) throw DomainException.Validacao("tipo: use apartamento, casa, terreno ou sala_comercial.");
        if (Titulo is null) throw DomainException.Validacao("titulo: obrigatório.");
        if (Garantias?.Any(g => !Opcoes.Garantias.Contains(g)) == true)
            throw DomainException.Validacao($"Garantias aceitas: {string.Join(", ", Opcoes.Garantias)}.");

        return AplicarEm(new DadosDoImovel
        {
            Codigo = "",
            Finalidade = finalidade,
            Tipo = tipo,
            Titulo = Titulo,
            MostrarMapa = true,
            Situacao = Enums.Situacao.Pronto,
            Status = StatusImovel.Rascunho,
            Destaque = false,
            ProprietarioNome = "",
            Endereco = "",
        });
    }

    /// <summary>Aplica os campos informados sobre o estado atual (a finalidade não muda numa atualização).</summary>
    public DadosDoImovel AplicarEm(DadosDoImovel atual) => atual with
    {
        Tipo = Tipo ?? atual.Tipo,
        Titulo = Titulo ?? atual.Titulo,
        Descricao = Descricao ?? atual.Descricao,
        Preco = Preco ?? atual.Preco,
        Aluguel = Aluguel ?? atual.Aluguel,
        Condominio = Condominio ?? atual.Condominio,
        Iptu = Iptu ?? atual.Iptu,
        EntradaAPartir = EntradaAPartir ?? atual.EntradaAPartir,
        BairroId = BairroId ?? atual.BairroId,
        MostrarMapa = MostrarMapa ?? atual.MostrarMapa,
        Quartos = Quartos ?? atual.Quartos,
        Suites = Suites ?? atual.Suites,
        Banheiros = Banheiros ?? atual.Banheiros,
        Vagas = Vagas ?? atual.Vagas,
        Area = Area ?? atual.Area,
        Andar = Andar ?? atual.Andar,
        Diferenciais = Diferenciais ?? atual.Diferenciais,
        Mcmv = Mcmv ?? atual.Mcmv,
        EntradaParcelada = EntradaParcelada ?? atual.EntradaParcelada,
        AceitaFgts = AceitaFgts ?? atual.AceitaFgts,
        Situacao = Situacao ?? atual.Situacao,
        PrevisaoEntrega = PrevisaoEntrega ?? atual.PrevisaoEntrega,
        Mobiliado = Mobiliado ?? atual.Mobiliado,
        AceitaPet = AceitaPet ?? atual.AceitaPet,
        Garantias = Garantias ?? atual.Garantias,
        VideoUrl = VideoUrl ?? atual.VideoUrl,
        ProprietarioNome = ProprietarioNome ?? atual.ProprietarioNome,
        Endereco = Endereco ?? atual.Endereco,
    };
}
