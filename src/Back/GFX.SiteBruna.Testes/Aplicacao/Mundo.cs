using System.Reflection;
using GFX.SiteBruna.Aplicacao.DTOs.Imagens;
using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFX.SiteBruna.Testes.Aplicacao;

/// <summary>
/// "Mundo" em memória para testar os casos de uso sem banco. Implementa todas as interfaces e imita o EF Core:
/// leituras devolvem cópias rastreadas; <c>Commit</c> grava as cópias (código único, cascata ao excluir);
/// <c>Descartar</c> esquece o que não foi gravado; reservas e fila gravam na hora, como os UPDATEs atômicos do Dados.
/// O relógio só anda quando o teste manda (e um tique a cada leitura, como o <c>atualizado_em</c> do banco).
/// </summary>
public sealed class Mundo : IUnitOfWork, IRelogio, IEspera, ICacheDoSite, IArmazenamentoDeArquivos, IProcessadorDeImagens,
    IEnderecosDoSite, ICanalWhatsapp
{
    public const string TelefoneBruna = "5549990000000";
    public const string TelefoneAgencia = "5549911111111";
    public const string TelefoneDesativado = "5549922222222";

    /// <summary>JPEG pela assinatura, mas que o processador não consegue ler.</summary>
    public static readonly byte[] ImagemCorrompida = [0xFF, 0xD8, 0xFF, 0xDB, .. new byte[16]];

    /// <summary>JPEG válido para o processador do mundo.</summary>
    public static byte[] Jpeg() => [0xFF, 0xD8, 0xFF, 0xE0, .. new byte[16]];

    private DateTimeOffset _agora = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
    private int _proximoCodigo = 200;

    // "Banco": o que já foi gravado
    private readonly Dictionary<Guid, Imovel> _imoveis = [];
    private readonly Dictionary<Guid, FotoImovel> _fotos = [];
    private readonly Dictionary<Guid, DadosInternosImovel> _internos = [];
    private readonly Dictionary<Guid, Bairro> _bairros = [];
    private readonly Dictionary<Guid, Operador> _operadores = [];
    private readonly Dictionary<Guid, AcaoPendente> _acoes = [];
    private readonly Dictionary<Guid, RegistroDeAuditoria> _auditoria = [];
    private readonly List<MensagemDoWhatsapp> _fila = [];
    private Configuracoes? _configuracoes;

    // Unidade de trabalho: cópias rastreadas, inclusões e exclusões ainda não gravadas
    private readonly Dictionary<(Type, Guid), object> _rastreados = [];
    private readonly List<(Type Tipo, Guid Id, object Entidade)> _incluidos = [];
    private readonly List<(Type Tipo, Guid Id)> _excluidos = [];

    public Mundo()
    {
        foreach (var nome in new[] { "Alvorada", "Centro", "Matriz", "Santa Gema", "Universitário" })
        {
            var bairro = new Bairro(nome);
            _bairros[bairro.Id] = bairro;
        }
        foreach (var (telefone, nome) in new[] { (TelefoneBruna, "Bruna"), (TelefoneAgencia, "Agência"), (TelefoneDesativado, "Ex") })
        {
            var op = new Operador(telefone, nome, _agora);
            if (telefone == TelefoneDesativado) op.Desativar();
            _operadores[op.Id] = op;
        }

        var imagens = new ImagemService(Imoveis, this, this);
        ImovelService = new ImovelService(Imoveis, Fotos, Internos, Bairros, this, this, this, this, imagens);
        Integracao = new IntegracaoService(Operadores, Acoes, Auditoria, Imoveis, Fotos, Internos, Bairros, this, this, this, this, ImovelService);
        Assistente = new AssistenteService(Integracao, this, Fila, Operadores, this, this, this, NullLogger<AssistenteService>.Instance);
        ImagemService = imagens;
        ImovelDoSite = new ImovelDoSiteService(DoSite, Fotos, Bairros, Configuracoes, this);
    }

    // ---------------------------------------------------------------------------------------------
    // Serviços prontos para os testes
    // ---------------------------------------------------------------------------------------------

    public ImagemService ImagemService { get; }
    public ImovelService ImovelService { get; }
    internal IntegracaoService Integracao { get; }
    public AssistenteService Assistente { get; }
    public ImovelDoSiteService ImovelDoSite { get; }

    public IImovelRepository Imoveis => new RepositorioDeImoveis(this);
    public IFotoRepository Fotos => new RepositorioDeFotos(this);
    public IDadosInternosRepository Internos => new RepositorioDeInternos(this);
    public IBairroRepository Bairros => new RepositorioDeBairros(this);
    public IOperadorRepository Operadores => new RepositorioDeOperadores(this);
    public IAcaoPendenteRepository Acoes => new RepositorioDeAcoes(this);
    public IAuditoriaRepository Auditoria => new RepositorioDeAuditoria(this);
    public IFilaDeMensagens Fila => new FilaEmMemoria(this);
    public IImovelDoSiteRepository DoSite => new RepositorioDoSite(this);
    public IConfiguracoesRepository Configuracoes => new RepositorioDeConfiguracoes(this);

    // ---------------------------------------------------------------------------------------------
    // Estado para conferir nos testes (sempre o que foi gravado)
    // ---------------------------------------------------------------------------------------------

    public int Invalidacoes { get; private set; }
    public Dictionary<string, byte[]> ArquivosEnviados { get; } = [];
    public List<string> ArquivosRemovidos { get; } = [];
    public List<(string Telefone, string Texto, IReadOnlyList<BotaoDoWhatsapp>? Botoes)> Enviadas { get; } = [];
    public Dictionary<string, byte[]> Midias { get; } = [];
    /// <summary>Executado uma vez durante a próxima espera (simula mensagens chegando no meio dela).</summary>
    public Func<Task>? AoEsperar { get; set; }
    /// <summary>Faz o próximo processamento de imagem lançar um erro inesperado.</summary>
    public bool FalharProcessamento { get; set; }

    public IReadOnlyCollection<Imovel> ImoveisGravados => _imoveis.Values;
    public IReadOnlyCollection<FotoImovel> FotosGravadas => _fotos.Values;
    public IReadOnlyCollection<AcaoPendente> AcoesGravadas => _acoes.Values;
    public IReadOnlyCollection<RegistroDeAuditoria> AuditoriaGravada => _auditoria.Values;
    public IReadOnlyList<MensagemDoWhatsapp> FilaGravada => _fila;

    public Imovel Imovel(string codigo) => _imoveis.Values.Single(i => i.Codigo == codigo);
    public DadosInternosImovel? InternosDe(Guid imovelId) => _internos.GetValueOrDefault(imovelId);
    public IReadOnlyList<FotoImovel> FotosDe(Guid imovelId) => [.. _fotos.Values.Where(f => f.ImovelId == imovelId).OrderBy(f => f.Ordem)];
    public Bairro Bairro(string nome) => _bairros.Values.Single(b => b.Nome == nome);
    public Operador Operador(string telefone) => _operadores.Values.Single(o => o.Telefone == telefone);

    public void Avancar(TimeSpan tempo) => _agora += tempo;

    /// <summary>Grava as configurações com o selo de vendido durando <paramref name="dias"/> (o padrão é 30).</summary>
    public void DefinirDiasDoSelo(int dias)
    {
        var configuracoes = new Configuracoes();
        configuracoes.Atualizar(new DadosDasConfiguracoes
        {
            Whatsapp = "5549999304262", MensagemPadrao = "Olá, Bruna!", Cidade = "Videira/SC", DiasSeloVendido = dias,
        }, Agora);
        _configuracoes = configuracoes;
    }

    /// <summary>Grava um imóvel direto no "banco", com <paramref name="quantasFotos"/> fotos.</summary>
    public Imovel CriarImovel(DadosDoImovel dados, int quantasFotos = 0, string? bairro = null)
    {
        var imovel = new global::GFX.SiteBruna.Modelo.Entities.Imovel(
            dados with { BairroId = bairro is null ? dados.BairroId : Bairro(bairro).Id },
            $"BA{_proximoCodigo++}", bairro, OrigemImovel.Painel, Agora);
        _imoveis[imovel.Id] = imovel;
        for (var i = 0; i < quantasFotos; i++)
        {
            var foto = new FotoImovel(imovel.Id, $"https://storage.teste/imoveis/{imovel.Id}/{i}.webp",
                $"imoveis/{imovel.Id}/{i}.webp", i, i == 0);
            _fotos[foto.Id] = foto;
        }
        return imovel;
    }

    public static DadosDoImovel Venda(string titulo = "Apartamento 2 quartos com sacada no Centro", StatusImovel status = StatusImovel.Ativo) => new()
    {
        Finalidade = Finalidade.Venda,
        Tipo = TipoImovel.Apartamento,
        Titulo = titulo,
        Preco = 365_000,
        Quartos = 2,
        Banheiros = 1,
        Vagas = 1,
        Status = status,
    };

    /// <summary>Mensagem como o webhook entrega.</summary>
    public MensagemRecebida Mensagem(ConteudoMensagem conteudo, string telefone = TelefoneBruna) =>
        new($"wamid.{Guid.NewGuid():N}", telefone, Agora, conteudo);

    /// <summary>Recebe e processa, como o webhook faz.</summary>
    public async Task Enviar(params MensagemRecebida[] mensagens)
    {
        foreach (var telefone in await Assistente.Receber(mensagens)) await Assistente.ProcessarFila(telefone);
    }

    public Task Texto(string texto, string telefone = TelefoneBruna) => Enviar(Mensagem(new ConteudoMensagem.Texto(texto), telefone));

    public Task Foto(string? legenda = null, string telefone = TelefoneBruna)
    {
        var midia = $"midia-{Midias.Count + 1}";
        Midias[midia] = Jpeg();
        return Enviar(Mensagem(new ConteudoMensagem.Imagem(midia, "image/jpeg", legenda), telefone));
    }

    public Task Botao(string id) => Enviar(Mensagem(new ConteudoMensagem.Botao(id, null)));

    // ---------------------------------------------------------------------------------------------
    // IUnitOfWork
    // ---------------------------------------------------------------------------------------------

    public Task Commit(CancellationToken ct = default)
    {
        foreach (var (tipo, id, entidade) in _incluidos) _rastreados[(tipo, id)] = entidade;

        var codigos = _rastreados.Values.OfType<Imovel>().Concat(_imoveis.Values)
            .GroupBy(i => i.Id).Select(g => g.First())
            .Where(i => !_excluidos.Contains((typeof(Imovel), i.Id)))
            .GroupBy(i => i.Codigo).Where(g => g.Count() > 1);
        if (codigos.Any()) throw new DomainException(CodigoErro.CodigoDuplicado, "Já existe um imóvel com esse código.");

        foreach (var ((tipo, id), entidade) in _rastreados) Tabela(tipo)[id] = Clonar(entidade);
        foreach (var (tipo, id) in _excluidos)
        {
            // Como o EF: excluído e gravado deixa de ser acompanhado (inclusive o que saiu em cascata)
            Tabela(tipo).Remove(id);
            _rastreados.Remove((tipo, id));
            if (tipo != typeof(Imovel)) continue;
            foreach (var foto in _fotos.Values.Where(f => f.ImovelId == id).ToList())
            {
                _fotos.Remove(foto.Id);
                _rastreados.Remove((typeof(FotoImovel), foto.Id));
            }
            _internos.Remove(id);
            _rastreados.Remove((typeof(DadosInternosImovel), id));
        }
        _incluidos.Clear();
        _excluidos.Clear();
        return Task.CompletedTask;
    }

    public void Descartar()
    {
        _rastreados.Clear();
        _incluidos.Clear();
        _excluidos.Clear();
    }

    // ---------------------------------------------------------------------------------------------
    // Demais interfaces técnicas
    // ---------------------------------------------------------------------------------------------

    public DateTimeOffset Agora => _agora += TimeSpan.FromTicks(1);

    public async Task Esperar(TimeSpan tempo)
    {
        _agora += tempo;
        var acao = AoEsperar;
        AoEsperar = null;
        if (acao is not null) await acao();
    }

    public Task Invalidar()
    {
        Invalidacoes++;
        return Task.CompletedTask;
    }

    public Task<string> Enviar(string caminho, byte[] conteudo, string tipo)
    {
        ArquivosEnviados[caminho] = conteudo;
        return Task.FromResult($"https://storage.teste/{caminho}");
    }

    public Task Remover(IReadOnlyCollection<string> caminhos)
    {
        ArquivosRemovidos.AddRange(caminhos);
        return Task.CompletedTask;
    }

    public Task<ImagemProcessada> Processar(byte[] conteudo)
    {
        if (FalharProcessamento)
        {
            FalharProcessamento = false;
            throw new InvalidOperationException("falha simulada no processamento");
        }
        if (conteudo.AsSpan().SequenceEqual(ImagemCorrompida))
            throw DomainException.Validacao("Não consegui ler essa imagem. Tente enviar de novo ou em outro formato.");
        return Task.FromResult(new ImagemProcessada([.. "WEBP"u8], "image/webp", 1920, 1080));
    }

    public string PaginaDoImovel(string slug, string codigo) => $"https://site.teste/imovel/{codigo.ToLowerInvariant()}-{slug}";

    public string PreviaNoPainel(Guid imovelId) => $"https://site.teste/admin/imoveis/{imovelId}/preview";

    public Task EnviarTexto(string telefone, string texto)
    {
        Enviadas.Add((telefone, texto, null));
        return Task.CompletedTask;
    }

    public Task EnviarBotoes(string telefone, string texto, IReadOnlyList<BotaoDoWhatsapp> botoes)
    {
        Enviadas.Add((telefone, texto, botoes));
        return Task.CompletedTask;
    }

    public Task<byte[]> BaixarMidia(string midiaId) =>
        Midias.TryGetValue(midiaId, out var conteudo)
            ? Task.FromResult(conteudo)
            : throw DomainException.Validacao("Não consegui baixar a foto. Envie de novo.");

    // ---------------------------------------------------------------------------------------------
    // Rastreamento
    // ---------------------------------------------------------------------------------------------

    private System.Collections.IDictionary Tabela(Type tipo) => tipo.Name switch
    {
        nameof(Imovel) => _imoveis,
        nameof(FotoImovel) => _fotos,
        nameof(DadosInternosImovel) => _internos,
        nameof(Bairro) => _bairros,
        nameof(Operador) => _operadores,
        nameof(AcaoPendente) => _acoes,
        nameof(RegistroDeAuditoria) => _auditoria,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo.Name, null),
    };

    /// <summary>Devolve a cópia rastreada (lê do "banco" na primeira vez), como o DbContext.</summary>
    private T? Rastrear<T>(Guid id, Dictionary<Guid, T> tabela) where T : class
    {
        if (_excluidos.Contains((typeof(T), id))) return null;
        if (_rastreados.TryGetValue((typeof(T), id), out var rastreado)) return (T)rastreado;
        if (!tabela.TryGetValue(id, out var gravado)) return null;
        var copia = Clonar(gravado);
        _rastreados[(typeof(T), id)] = copia;
        return copia;
    }

    private void Incluir<T>(Guid id, T entidade) where T : class => _incluidos.Add((typeof(T), id, entidade));

    private static readonly MethodInfo MemberwiseCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>Cópia da entidade com listas próprias (alterar a cópia não mexe no que está gravado).</summary>
    private static T Clonar<T>(T entidade) where T : class
    {
        var copia = (T)MemberwiseCloneMethod.Invoke(entidade, null)!;
        foreach (var p in entidade.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) && p.GetValue(entidade) is { } lista)
                p.SetValue(copia, Activator.CreateInstance(p.PropertyType, lista));
        return copia;
    }

    private static void Definir(object entidade, string propriedade, object? valor) =>
        entidade.GetType().GetProperty(propriedade)!.SetValue(entidade, valor);

    // ---------------------------------------------------------------------------------------------
    // Repositórios
    // ---------------------------------------------------------------------------------------------

    private sealed class RepositorioDeImoveis(Mundo m) : IImovelRepository
    {
        public Task<Imovel?> GetById(Guid id) => Task.FromResult(m.Rastrear(id, m._imoveis));

        public Task<Imovel?> GetByCodigo(string codigo) =>
            Task.FromResult(m._imoveis.Values.FirstOrDefault(i => i.Codigo == codigo) is { } i ? m.Rastrear(i.Id, m._imoveis) : null);

        public Task<IReadOnlyList<string>> GetCodigosRecentes(int limite) =>
            Task.FromResult<IReadOnlyList<string>>([.. m._imoveis.Values.OrderByDescending(i => i.CriadoEm).Take(limite).Select(i => i.Codigo)]);

        public Task<bool> ExistsByCodigo(string codigo, Guid? excetoId) =>
            Task.FromResult(m._imoveis.Values.Any(i => i.Codigo == codigo && i.Id != excetoId));

        public Task<IReadOnlyList<Imovel>> Pesquisar(FiltroDeImoveis filtro) =>
            Task.FromResult<IReadOnlyList<Imovel>>(
            [
                .. m._imoveis.Values
                    .Where(i => string.IsNullOrWhiteSpace(filtro.Busca)
                                || i.Codigo.Contains(filtro.Busca.Trim(), StringComparison.OrdinalIgnoreCase)
                                || i.Titulo.Contains(filtro.Busca.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Where(i => filtro.Status is null || i.Status == filtro.Status)
                    .Where(i => filtro.Finalidade is null || i.Finalidade == filtro.Finalidade)
                    .Where(i => filtro.BairroId is null || i.BairroId == filtro.BairroId)
                    .OrderByDescending(i => i.AtualizadoEm)
                    .Take(filtro.Limite),
            ]);

        public Task<string> ProximoCodigo() => Task.FromResult($"BA{m._proximoCodigo++}");

        public void Add(Imovel imovel) => m.Incluir(imovel.Id, imovel);

        public void Delete(Imovel imovel) => m._excluidos.Add((typeof(Imovel), imovel.Id));
    }

    private sealed class RepositorioDeFotos(Mundo m) : IFotoRepository
    {
        public Task<List<FotoImovel>> GetDoImovel(Guid imovelId) =>
            Task.FromResult(m._fotos.Values.Where(f => f.ImovelId == imovelId)
                .Select(f => m.Rastrear(f.Id, m._fotos)).OfType<FotoImovel>().OrderBy(f => f.Ordem).ToList());

        public Task<IReadOnlyDictionary<Guid, string>> GetCapas(IReadOnlyCollection<Guid> imovelIds) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(m._fotos.Values
                .Where(f => f.Capa && imovelIds.Contains(f.ImovelId))
                .GroupBy(f => f.ImovelId)
                .ToDictionary(g => g.Key, g => g.First().Url));

        public Task<IReadOnlySet<string>> CaminhosEmUso(IReadOnlyCollection<string> caminhos) =>
            Task.FromResult<IReadOnlySet<string>>(m._fotos.Values.Select(f => f.CaminhoNoStorage).OfType<string>()
                .Where(caminhos.Contains).ToHashSet());

        public void Add(FotoImovel foto) => m.Incluir(foto.Id, foto);

        public void Delete(FotoImovel foto) => m._excluidos.Add((typeof(FotoImovel), foto.Id));
    }

    private sealed class RepositorioDeInternos(Mundo m) : IDadosInternosRepository
    {
        public Task<DadosInternosImovel?> GetByImovel(Guid imovelId)
        {
            var incluido = m._incluidos.Select(x => x.Entidade).OfType<DadosInternosImovel>().FirstOrDefault(d => d.ImovelId == imovelId);
            return Task.FromResult(incluido ?? m.Rastrear(imovelId, m._internos));
        }

        public void Add(DadosInternosImovel dados) => m.Incluir(dados.ImovelId, dados);
    }

    private sealed class RepositorioDeBairros(Mundo m) : IBairroRepository
    {
        public Task<IReadOnlyList<Bairro>> GetAll() => Task.FromResult<IReadOnlyList<Bairro>>([.. m._bairros.Values.OrderBy(b => b.Nome)]);

        public Task<Bairro?> GetById(Guid id) => Task.FromResult(m.Rastrear(id, m._bairros));

        public Task<bool> ExistsBySlug(string slug, Guid? excetoId) =>
            Task.FromResult(m._bairros.Values.Any(b => b.Slug == slug && b.Id != excetoId));

        public Task<int> ContarImoveis(Guid bairroId) => Task.FromResult(m._imoveis.Values.Count(i => i.BairroId == bairroId));

        public void Add(Bairro bairro) => m.Incluir(bairro.Id, bairro);

        public void Delete(Bairro bairro) => m._excluidos.Add((typeof(Bairro), bairro.Id));
    }

    /// <summary>As mesmas consultas do <c>ImovelDoSiteRepository</c> do Dados, sobre o que está gravado (sem rastrear).</summary>
    private sealed class RepositorioDoSite(Mundo m) : IImovelDoSiteRepository
    {
        private IEnumerable<Imovel> Ativos => m._imoveis.Values.Where(i => i.Status == StatusImovel.Ativo);

        public Task<(IReadOnlyList<Imovel> Itens, int Total)> Pesquisar(
            FiltroDoSite filtro, Guid? bairroId, DateTimeOffset agora, int diasSeloVendido)
        {
            var venda = filtro.Finalidade == Finalidade.Venda;
            decimal? Preco(Imovel i) => venda ? i.Preco : i.Aluguel;
            var visivel = global::GFX.SiteBruna.Modelo.Entities.Imovel.VisivelNoSite(agora, diasSeloVendido).Compile();
            var encontrados = m._imoveis.Values
                .Where(visivel)
                .Where(i => i.Finalidade == filtro.Finalidade)
                .Where(i => filtro.Tipo is null || i.Tipo == filtro.Tipo)
                .Where(i => bairroId is null || i.BairroId == bairroId)
                .Where(i => filtro.PrecoMinimo is null || Preco(i) >= filtro.PrecoMinimo)
                .Where(i => filtro.PrecoMaximo is null || Preco(i) <= filtro.PrecoMaximo)
                .Where(i => filtro.Quartos is null || i.Quartos >= filtro.Quartos)
                .Where(i => filtro.Vagas is null || i.Vagas >= filtro.Vagas)
                .Where(i => filtro.Situacao is null || i.Situacao == filtro.Situacao)
                .Where(i => (!filtro.Mcmv || i.Mcmv) && (!filtro.EntradaParcelada || i.EntradaParcelada)
                            && (!filtro.AceitaFgts || i.AceitaFgts) && (!filtro.Mobiliado || i.Mobiliado)
                            && (!filtro.AceitaPet || i.AceitaPet))
                .ToList();

            var ordenada = encontrados.OrderBy(i => i.VendidoEm != null).ThenByDescending(i => i.VendidoEm);
            ordenada = filtro.Ordem switch
            {
                OrdemDoSite.MenorPreco => ordenada.ThenBy(i => Preco(i) == null).ThenBy(Preco),
                OrdemDoSite.MaiorPreco => ordenada.ThenBy(i => Preco(i) == null).ThenByDescending(Preco),
                _ => ordenada,
            };
            IReadOnlyList<Imovel> pagina =
            [
                .. ordenada.ThenByDescending(i => i.CriadoEm).ThenBy(i => i.Id)
                    .Skip(filtro.Pular).Take(FiltroDoSite.ItensPorPagina).Select(Clonar),
            ];
            return Task.FromResult((pagina, encontrados.Count));
        }

        public Task<Imovel?> GetVisivelByCodigo(string codigo, DateTimeOffset agora, int diasSeloVendido) =>
            Task.FromResult(m._imoveis.Values.FirstOrDefault(i => i.Codigo == codigo && i.EstaVisivelNoSite(agora, diasSeloVendido))
                is { } imovel ? Clonar(imovel) : null);

        public Task<IReadOnlyList<Imovel>> Destaques(int limite) =>
            Task.FromResult<IReadOnlyList<Imovel>>([.. Ativos.Where(i => i.Destaque).OrderByDescending(i => i.CriadoEm).Take(limite).Select(Clonar)]);

        public Task<IReadOnlyList<Imovel>> Recentes(Finalidade? finalidade, bool somenteMcmv, int limite) =>
            Task.FromResult<IReadOnlyList<Imovel>>(
            [
                .. Ativos.Where(i => (finalidade is null || i.Finalidade == finalidade) && (!somenteMcmv || i.Mcmv))
                    .OrderByDescending(i => i.CriadoEm).Take(limite).Select(Clonar),
            ]);

        public Task<IReadOnlyList<Imovel>> CandidatosASemelhantes(Imovel referencia, int limite) =>
            Task.FromResult<IReadOnlyList<Imovel>>(
            [
                .. Ativos.Where(i => i.Id != referencia.Id && i.Finalidade == referencia.Finalidade
                                     && (i.Tipo == referencia.Tipo || (referencia.BairroId != null && i.BairroId == referencia.BairroId)))
                    .OrderByDescending(i => i.CriadoEm).Take(limite).Select(Clonar),
            ]);

        public Task<IReadOnlyList<(string Codigo, string Slug, DateTimeOffset AtualizadoEm)>> ParaSitemap(int limite) =>
            Task.FromResult<IReadOnlyList<(string, string, DateTimeOffset)>>(
                [.. Ativos.OrderByDescending(i => i.AtualizadoEm).Take(limite).Select(i => (i.Codigo, i.Slug, i.AtualizadoEm))]);
    }

    private sealed class RepositorioDeConfiguracoes(Mundo m) : IConfiguracoesRepository
    {
        public Task<Configuracoes?> Get() => Task.FromResult(m._configuracoes);

        public void Add(Configuracoes configuracoes) => m._configuracoes = configuracoes;
    }

    private sealed class RepositorioDeOperadores(Mundo m) : IOperadorRepository
    {
        public Task<Operador?> GetByTelefone(string telefone) =>
            Task.FromResult(m._operadores.Values.FirstOrDefault(o => o.Telefone == telefone) is { } o ? m.Rastrear(o.Id, m._operadores) : null);

        public void Add(Operador operador) => m.Incluir(operador.Id, operador);

        public Task<bool> Reservar(Guid operadorId, string token, DateTimeOffset ate, DateTimeOffset agora)
        {
            // Atômico: grava na hora e reflete na cópia rastreada (o Commit seguinte não desfaz a reserva)
            var reservou = m._operadores[operadorId].Reservar(token, ate, agora);
            if (reservou && m._rastreados.TryGetValue((typeof(Operador), operadorId), out var rastreado))
                ((Operador)rastreado).Reservar(token, ate, agora);
            return Task.FromResult(reservou);
        }

        public Task Liberar(Guid operadorId, string token)
        {
            m._operadores[operadorId].Liberar(token);
            if (m._rastreados.TryGetValue((typeof(Operador), operadorId), out var rastreado)) ((Operador)rastreado).Liberar(token);
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioDeAcoes(Mundo m) : IAcaoPendenteRepository
    {
        public Task<AcaoPendente?> GetById(Guid id) => Task.FromResult(m.Rastrear(id, m._acoes));

        public Task<IReadOnlyList<AcaoPendente>> GetPendentes(Guid operadorId, DateTimeOffset agora) =>
            Task.FromResult<IReadOnlyList<AcaoPendente>>(
            [
                .. m._acoes.Values
                    .Where(a => a.OperadorId == operadorId && a.Status == StatusAcao.Pendente && a.ExpiraEm > agora)
                    .OrderByDescending(a => a.CriadoEm)
                    .Select(a => m.Rastrear(a.Id, m._acoes)!),
            ]);

        public void Add(AcaoPendente acao) => m.Incluir(acao.Id, acao);

        public Task<bool> ReservarConfirmacao(Guid id, DateTimeOffset agora)
        {
            if (!m._acoes.TryGetValue(id, out var gravada) || gravada.Status != StatusAcao.Pendente || gravada.ExpiraEm <= agora)
                return Task.FromResult(false);
            Definir(gravada, nameof(AcaoPendente.Status), StatusAcao.Confirmada);
            return Task.FromResult(true);
        }

        public Task DevolverParaPendente(Guid id)
        {
            Definir(m._acoes[id], nameof(AcaoPendente.Status), StatusAcao.Pendente);
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioDeAuditoria(Mundo m) : IAuditoriaRepository
    {
        public void Add(RegistroDeAuditoria registro) => m.Incluir(registro.Id, registro);

        public Task<RegistroDeAuditoria?> GetUltimaConfirmada(Guid operadorId) =>
            Task.FromResult(m._auditoria.Values
                .Where(r => r.OperadorId == operadorId && r.Operacao == RegistroDeAuditoria.ConfirmarAcao && r.Sucesso)
                .OrderByDescending(r => r.CriadoEm).FirstOrDefault());
    }

    private sealed class FilaEmMemoria(Mundo m) : IFilaDeMensagens
    {
        public Task<bool> Inserir(MensagemDoWhatsapp mensagem)
        {
            if (m._fila.Any(x => x.Wamid == mensagem.Wamid)) return Task.FromResult(false);
            m._fila.Add(Clonar(mensagem));
            return Task.FromResult(true);
        }

        public Task<MensagemDoWhatsapp?> ProximaPendente(Guid operadorId) =>
            Task.FromResult(m._fila.Where(x => x.OperadorId == operadorId && x.Status == StatusMensagem.Pendente)
                .OrderBy(x => x.RecebidaEm).Select(Clonar).FirstOrDefault());

        public Task Gravar(MensagemDoWhatsapp mensagem)
        {
            m._fila[m._fila.FindIndex(x => x.Id == mensagem.Id)] = Clonar(mensagem);
            return Task.CompletedTask;
        }

        public Task<int> ContarDesde(Guid operadorId, DateTimeOffset desde) =>
            Task.FromResult(m._fila.Count(x => x.OperadorId == operadorId && x.RecebidaEm >= desde));
    }
}
