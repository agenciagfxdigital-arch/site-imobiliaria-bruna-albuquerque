using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>
/// Imóvel anunciado no site. Tudo o que fica aqui é público: dados internos (proprietário, endereço exato)
/// ficam em <see cref="DadosInternosImovel"/>.
/// </summary>
public sealed partial class Imovel
{
    public const int TamanhoMaximoTitulo = 140;

    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public Finalidade Finalidade { get; private set; }
    public TipoImovel Tipo { get; private set; }
    public string Titulo { get; private set; } = "";
    public string Descricao { get; private set; } = "";
    public decimal? Preco { get; private set; }
    public decimal? Aluguel { get; private set; }
    public decimal? Condominio { get; private set; }
    public decimal? Iptu { get; private set; }
    public decimal? EntradaAPartir { get; private set; }
    public Guid? BairroId { get; private set; }
    public bool MostrarMapa { get; private set; }
    public int Quartos { get; private set; }
    public int Suites { get; private set; }
    public int Banheiros { get; private set; }
    public int Vagas { get; private set; }
    public decimal? Area { get; private set; }
    public string Andar { get; private set; } = "";
    public List<string> Diferenciais { get; private set; } = [];
    public bool Mcmv { get; private set; }
    public bool EntradaParcelada { get; private set; }
    public bool AceitaFgts { get; private set; }
    public Situacao Situacao { get; private set; }
    public string PrevisaoEntrega { get; private set; } = "";
    public bool Mobiliado { get; private set; }
    public bool AceitaPet { get; private set; }
    public List<string> Garantias { get; private set; } = [];
    public string VideoUrl { get; private set; } = "";
    public bool Destaque { get; private set; }
    public StatusImovel Status { get; private set; }
    /// <summary>Quando passou a vendido/alugado (selo "Vendido" no site por alguns dias).</summary>
    public DateTimeOffset? VendidoEm { get; private set; }
    public OrigemImovel Origem { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private Imovel() { } // EF

    /// <param name="codigoGerado">Próximo código da sequência; usado só quando <paramref name="dados"/> não traz código.</param>
    public Imovel(DadosDoImovel dados, string? codigoGerado, string? nomeDoBairro, OrigemImovel origem, DateTimeOffset agora)
    {
        if (dados.PrecisaDeCodigoGerado && string.IsNullOrWhiteSpace(codigoGerado))
            throw new InvalidOperationException("Imóvel sem código precisa do próximo código da sequência.");
        Id = Guid.CreateVersion7();
        Origem = origem;
        CriadoEm = agora;
        Codigo = dados.PrecisaDeCodigoGerado ? codigoGerado!.ToUpperInvariant() : "";
        Aplicar(dados, nomeDoBairro, agora);
    }

    /// <summary>Edição pelo painel (ou ação confirmada do WhatsApp). Sem código informado, mantém o atual.</summary>
    public void Atualizar(DadosDoImovel dados, string? nomeDoBairro, DateTimeOffset agora) => Aplicar(dados, nomeDoBairro, agora);

    /// <summary>Troca o status. Ao virar vendido/alugado, marca a data (para o selo); ao sair, apaga.</summary>
    public void AlterarStatus(StatusImovel novo, DateTimeOffset agora)
    {
        var vendido = novo is StatusImovel.Vendido or StatusImovel.Alugado;
        if (vendido && (Status != novo || VendidoEm is null)) VendidoEm = agora;
        else if (!vendido) VendidoEm = null;
        Status = novo;
        AtualizadoEm = agora;
    }

    public void DefinirDestaque(bool destaque, DateTimeOffset agora)
    {
        Destaque = destaque;
        AtualizadoEm = agora;
    }

    /// <summary>Estado atual como entrada completa (com os dados internos), para aplicar mudanças e comparar.</summary>
    public DadosDoImovel ParaDados(DadosInternosImovel? internos) => new()
    {
        Codigo = Codigo,
        Finalidade = Finalidade,
        Tipo = Tipo,
        Titulo = Titulo,
        Descricao = Descricao,
        Preco = Preco,
        Aluguel = Aluguel,
        Condominio = Condominio,
        Iptu = Iptu,
        EntradaAPartir = EntradaAPartir,
        BairroId = BairroId,
        MostrarMapa = MostrarMapa,
        Quartos = Quartos,
        Suites = Suites,
        Banheiros = Banheiros,
        Vagas = Vagas,
        Area = Area,
        Andar = Andar,
        Diferenciais = [.. Diferenciais],
        Mcmv = Mcmv,
        EntradaParcelada = EntradaParcelada,
        AceitaFgts = AceitaFgts,
        Situacao = Situacao,
        PrevisaoEntrega = PrevisaoEntrega,
        Mobiliado = Mobiliado,
        AceitaPet = AceitaPet,
        Garantias = [.. Garantias],
        VideoUrl = VideoUrl,
        Destaque = Destaque,
        Status = Status,
        ProprietarioNome = internos?.ProprietarioNome ?? "",
        Endereco = internos?.Endereco ?? "",
    };

    // --- Regras das propostas pelo WhatsApp ------------------------------------------------------

    public void GarantirQuePodeSerPublicado()
    {
        if (Status == StatusImovel.Ativo) throw new DomainException(CodigoErro.Conflito, $"O {Codigo} já está publicado.");
        if (Status is StatusImovel.Vendido or StatusImovel.Alugado)
            throw DomainException.Validacao(
                $"O {Codigo} está {Formatos.Rotulo(Status).ToLowerInvariant()}. Para reativar, peça a mudança de status.");
    }

    /// <summary>Confere a mudança de status pedida pelo WhatsApp (rascunho vai para o site só pela publicação).</summary>
    public void GarantirNovoStatusValido(StatusImovel novo)
    {
        if (novo == StatusImovel.Rascunho) throw DomainException.Validacao("status: use ativo, pausado, vendido ou alugado.");
        if (novo == Status)
            throw new DomainException(CodigoErro.Conflito, $"O {Codigo} já está como {Formatos.Rotulo(novo).ToLowerInvariant()}.");
        if (novo == StatusImovel.Vendido && Finalidade != Finalidade.Venda)
            throw DomainException.Validacao("Imóvel de aluguel não pode ser marcado como vendido.");
        if (novo == StatusImovel.Alugado && Finalidade != Finalidade.Aluguel)
            throw DomainException.Validacao("Imóvel de venda não pode ser marcado como alugado.");
        if (novo == StatusImovel.Ativo && Status == StatusImovel.Rascunho)
            throw DomainException.Validacao("Para colocar um rascunho no site, use a publicação.");
    }

    /// <summary>Pelo WhatsApp, só rascunho muda direto; o que está no site precisa de proposta e confirmação.</summary>
    public void GarantirRascunho()
    {
        if (Status != StatusImovel.Rascunho)
            throw new DomainException(CodigoErro.Conflito,
                $"O {Codigo} já está no site. Alterações nele precisam de confirmação: use a proposta de alteração.");
    }

    /// <summary>Ativo, ou vendido/alugado há menos de <paramref name="diasSeloVendido"/> dias (com o selo).</summary>
    public bool EstaVisivelNoSite(DateTimeOffset agora, int diasSeloVendido) =>
        Status == StatusImovel.Ativo
        || (Status is StatusImovel.Vendido or StatusImovel.Alugado
            && VendidoEm is { } vendidoEm
            && vendidoEm > agora.AddDays(-Math.Max(diasSeloVendido, 0)));

    /// <summary>Cópia: vira rascunho, sai dos destaques e ganha "(cópia)" no título, com código novo.</summary>
    public Imovel Copiar(string codigoGerado, DateTimeOffset agora)
    {
        var copia = (Imovel)MemberwiseClone();
        copia.Id = Guid.CreateVersion7();
        copia.Codigo = codigoGerado.ToUpperInvariant();
        copia.Titulo = Cortar($"{Titulo} (cópia)", TamanhoMaximoTitulo);
        copia.Status = StatusImovel.Rascunho;
        copia.Destaque = false;
        copia.VendidoEm = null;
        copia.Diferenciais = [.. Diferenciais];
        copia.Garantias = [.. Garantias];
        copia.CriadoEm = agora;
        copia.AtualizadoEm = agora;
        return copia;
    }

    /// <summary>Slug da URL: título + bairro (quando o título ainda não cita o bairro).</summary>
    public static string GerarSlug(string titulo, string? nomeDoBairro)
    {
        var incluiBairro = !string.IsNullOrWhiteSpace(nomeDoBairro)
            && !Texto.Slug(titulo).Contains(Texto.Slug(nomeDoBairro));
        return Texto.Slug(incluiBairro ? $"{titulo} {nomeDoBairro}" : titulo);
    }

    private void Aplicar(DadosDoImovel dados, string? nomeDoBairro, DateTimeOffset agora)
    {
        Validar(dados);
        var venda = dados.Finalidade == Finalidade.Venda;

        if (!dados.PrecisaDeCodigoGerado) Codigo = dados.Codigo!.Trim().ToUpperInvariant();
        Finalidade = dados.Finalidade;
        Tipo = dados.Tipo;
        Titulo = dados.Titulo.Trim();
        Descricao = dados.Descricao;
        // Campos que não se aplicam à finalidade são limpos, para não confundir o site.
        Preco = venda ? dados.Preco : null;
        Aluguel = venda ? null : dados.Aluguel;
        EntradaAPartir = venda ? dados.EntradaAPartir : null;
        Condominio = dados.Condominio;
        Iptu = dados.Iptu;
        BairroId = dados.BairroId;
        MostrarMapa = dados.MostrarMapa;
        Quartos = dados.Quartos;
        Suites = dados.Suites;
        Banheiros = dados.Banheiros;
        Vagas = dados.Vagas;
        Area = dados.Area;
        Andar = dados.Andar.Trim();
        Diferenciais = [.. dados.Diferenciais.Select(d => d.Trim()).Where(d => d.Length > 0)];
        Mcmv = dados.Mcmv;
        EntradaParcelada = dados.EntradaParcelada;
        AceitaFgts = dados.AceitaFgts;
        Situacao = dados.Situacao;
        PrevisaoEntrega = dados.PrevisaoEntrega.Trim();
        Mobiliado = dados.Mobiliado;
        AceitaPet = dados.AceitaPet;
        Garantias = [.. dados.Garantias.Select(g => g.Trim()).Where(g => g.Length > 0)];
        VideoUrl = dados.VideoUrl.Trim();
        Destaque = dados.Destaque;
        Slug = GerarSlug(Titulo, nomeDoBairro);
        AlterarStatus(dados.Status, agora);
    }

    /// <summary>Confere um estado completo sem criar o imóvel (ex.: o resultado de uma proposta pelo WhatsApp).</summary>
    public static void Validar(DadosDoImovel d)
    {
        if (!d.PrecisaDeCodigoGerado && !CodigoValido().IsMatch(d.Codigo!.Trim()))
            throw DomainException.Validacao("O código deve ter de 2 a 20 letras e números, sem espaços.");
        var titulo = d.Titulo.Trim();
        if (titulo.Length < 3) throw DomainException.Validacao("Escreva um título.");
        if (titulo.Length > TamanhoMaximoTitulo) throw DomainException.Validacao($"O título pode ter até {TamanhoMaximoTitulo} letras.");
        if (d.Descricao.Length > 10_000) throw DomainException.Validacao("A descrição pode ter até 10 mil letras.");

        (decimal? Valor, string Nome)[] valores =
            [(d.Preco, "O preço"), (d.Aluguel, "O aluguel"), (d.Condominio, "O condomínio"), (d.Iptu, "O IPTU"),
             (d.EntradaAPartir, "A entrada"), (d.Area, "A área")];
        foreach (var (valor, nome) in valores)
            if (valor < 0) throw DomainException.Validacao($"{nome} não pode ser negativo.");

        (int Valor, string Nome)[] contagens =
            [(d.Quartos, "quartos"), (d.Suites, "suítes"), (d.Banheiros, "banheiros"), (d.Vagas, "vagas")];
        foreach (var (valor, nome) in contagens)
            if (valor is < 0 or > 99) throw DomainException.Validacao($"O número de {nome} deve ficar entre 0 e 99.");

        if (d.Andar.Trim().Length > 40) throw DomainException.Validacao("O andar pode ter até 40 letras.");
        if (d.PrevisaoEntrega.Trim().Length > 60) throw DomainException.Validacao("A previsão de entrega pode ter até 60 letras.");
        if (d.VideoUrl.Trim().Length > 300) throw DomainException.Validacao("O link do vídeo pode ter até 300 letras.");
        ValidarLista(d.Diferenciais, 40, "diferenciais");
        ValidarLista(d.Garantias, 10, "garantias");
        DadosInternosImovel.ValidarTamanhos(d.ProprietarioNome, d.Endereco);
    }

    private static void ValidarLista(IReadOnlyList<string> itens, int maximo, string nome)
    {
        if (itens.Count > maximo) throw DomainException.Validacao($"Use até {maximo} {nome}.");
        if (itens.Any(i => i.Trim().Length > 60)) throw DomainException.Validacao($"Cada item de {nome} pode ter até 60 letras.");
    }

    private static string Cortar(string texto, int tamanho) => texto.Length > tamanho ? texto[..tamanho] : texto;

    [GeneratedRegex("^[A-Za-z0-9]{2,20}$")]
    private static partial Regex CodigoValido();
}
