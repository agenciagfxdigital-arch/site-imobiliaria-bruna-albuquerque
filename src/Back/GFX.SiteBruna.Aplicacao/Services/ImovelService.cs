using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>
/// Casos de uso de imóveis e fotos do painel. Não sabe quem está chamando: a autorização acontece antes, na Api.
/// O assistente do WhatsApp usa os métodos <c>internal</c> (mesmas regras, mesmo cache do site).
/// </summary>
public sealed class ImovelService(
    IImovelRepository imoveis,
    IFotoRepository fotos,
    IDadosInternosRepository internos,
    IBairroRepository bairros,
    IArmazenamentoDeArquivos arquivos,
    ICacheDoSite cache,
    IUnitOfWork unitOfWork,
    IRelogio relogio,
    ImagemService imagens)
{
    private readonly IImovelRepository _imoveis = imoveis;
    private readonly IFotoRepository _fotos = fotos;
    private readonly IDadosInternosRepository _internos = internos;
    private readonly IBairroRepository _bairros = bairros;
    private readonly IArmazenamentoDeArquivos _arquivos = arquivos;
    private readonly ICacheDoSite _cache = cache;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;
    private readonly ImagemService _imagens = imagens;

    /// <summary>Cria (sem id) ou atualiza (com id) um imóvel, com os dados internos à parte.</summary>
    public Task<ApiResponse<ImovelSalvoResponse>> Salvar(SalvarImovelRequest request) =>
        Resposta.De(async () =>
        {
            var imovel = await Salvar(request.Id, request.Dados, OrigemImovel.Painel, autor: null);
            return new ImovelSalvoResponse(imovel.Id, imovel.Codigo);
        });

    /// <summary>Proprietário, endereço exato e quem alterou (só para o painel; nunca para o site).</summary>
    public Task<ApiResponse<DadosInternosResponse>> DadosInternos(Guid imovelId) =>
        Resposta.De(async () => DadosInternosResponse.De(await _internos.GetByImovel(imovelId)));

    public Task<ApiResponse<IReadOnlyList<ImovelResumoResponse>>> Listar(FiltroDeImoveis filtro) =>
        Resposta.De(async () =>
        {
            var lista = await _imoveis.Pesquisar(filtro with { Limite = Math.Clamp(filtro.Limite, 1, 500) });
            var capas = await _fotos.GetCapas([.. lista.Select(i => i.Id)]);
            return (IReadOnlyList<ImovelResumoResponse>)[.. lista.Select(i => ImovelResumoResponse.De(i, capas.GetValueOrDefault(i.Id)))];
        });

    /// <summary>Imóvel completo para editar no painel (com proprietário e endereço exato).</summary>
    public Task<ApiResponse<ImovelDetalheResponse>> Obter(Guid id) =>
        Resposta.De(async () =>
        {
            var imovel = await ExigirImovel(id);
            var internos = await _internos.GetByImovel(id);
            var fotos = await _fotos.GetDoImovel(id);
            return new ImovelDetalheResponse(imovel.Id, imovel.Slug, imovel.ParaDados(internos), [.. fotos.Select(FotoResponse.De)],
                imovel.Origem, internos?.AtualizadoPor, imovel.VendidoEm, imovel.CriadoEm, imovel.AtualizadoEm);
        });

    /// <summary>Substitui a lista de fotos (ordem, capa, remoções e fotos novas já enviadas ao storage).</summary>
    public Task<ApiResponse<IReadOnlyList<FotoResponse>>> SalvarFotos(Guid imovelId, IReadOnlyList<FotoDesejada> desejadas) =>
        Resposta.De(async () =>
        {
            await ExigirImovel(imovelId);
            var atuais = await _fotos.GetDoImovel(imovelId);
            var plano = PlanejamentoDeFotos.Planejar(atuais, desejadas);
            var salvas = AplicarPlano(imovelId, atuais, plano);
            await _unitOfWork.Commit();
            await ApagarArquivosSemUso(plano.Removidas.Select(f => f.CaminhoNoStorage));
            await _cache.Invalidar();
            return (IReadOnlyList<FotoResponse>)[.. salvas.Select(FotoResponse.De)];
        });

    public Task<ApiResponse<bool>> AlterarStatus(Guid imovelId, StatusImovel status) =>
        Resposta.De(async () =>
        {
            (await ExigirImovel(imovelId)).AlterarStatus(status, _relogio.Agora);
            await _unitOfWork.Commit();
            await _cache.Invalidar();
        });

    public Task<ApiResponse<bool>> AlterarDestaque(Guid imovelId, bool destaque) =>
        Resposta.De(async () =>
        {
            (await ExigirImovel(imovelId)).DefinirDestaque(destaque, _relogio.Agora);
            await _unitOfWork.Commit();
            await _cache.Invalidar();
        });

    /// <summary>Cópia em rascunho, com o mesmo endereço e as mesmas fotos (que reaproveitam os arquivos do original).</summary>
    public Task<ApiResponse<ImovelSalvoResponse>> Duplicar(Guid imovelId) =>
        Resposta.De(async () =>
        {
            var agora = _relogio.Agora;
            var original = await ExigirImovel(imovelId);
            var copia = original.Copiar(await _imoveis.ProximoCodigo(), agora);
            _imoveis.Add(copia);
            if (await _internos.GetByImovel(original.Id) is { Endereco.Length: > 0 } internosOriginal)
                _internos.Add(internosOriginal.CopiarPara(copia.Id, agora));
            foreach (var foto in await _fotos.GetDoImovel(original.Id)) _fotos.Add(foto.CopiarPara(copia.Id));
            await _unitOfWork.Commit();
            return new ImovelSalvoResponse(copia.Id, copia.Codigo);
        });

    /// <summary>Exclui o imóvel (fotos e dados internos saem junto) e apaga os arquivos que ninguém mais usa.</summary>
    public Task<ApiResponse<bool>> Excluir(Guid imovelId) =>
        Resposta.De(async () =>
        {
            var imovel = await ExigirImovel(imovelId);
            var caminhos = (await _fotos.GetDoImovel(imovel.Id)).Select(f => f.CaminhoNoStorage).ToList();
            _imoveis.Delete(imovel);
            await _unitOfWork.Commit();
            await ApagarArquivosSemUso(caminhos);
            await _cache.Invalidar();
        });

    // ---------------------------------------------------------------------------------------------
    // Usados também pelo assistente do WhatsApp (lançam DomainException)
    // ---------------------------------------------------------------------------------------------

    internal async Task<Imovel> Salvar(Guid? id, DadosDoImovel dados, OrigemImovel origem, string? autor)
    {
        var agora = _relogio.Agora;
        var nomeDoBairro = await NomeDoBairro(dados.BairroId);
        if (!dados.PrecisaDeCodigoGerado && await _imoveis.ExistsByCodigo(dados.Codigo!.Trim().ToUpperInvariant(), id))
            throw new DomainException(CodigoErro.CodigoDuplicado, "Já existe um imóvel com esse código.");

        Imovel imovel;
        if (id is { } existente)
        {
            imovel = await ExigirImovel(existente);
            imovel.Atualizar(dados, nomeDoBairro, agora);
        }
        else
        {
            var codigoGerado = dados.PrecisaDeCodigoGerado ? await _imoveis.ProximoCodigo() : null;
            imovel = new Imovel(dados, codigoGerado, nomeDoBairro, origem, agora);
            _imoveis.Add(imovel);
        }

        if (dados.ProprietarioNome is not null || dados.Endereco is not null || autor is not null)
        {
            var internos = await _internos.GetByImovel(imovel.Id);
            if (internos is null)
            {
                internos = new DadosInternosImovel(imovel.Id, agora);
                _internos.Add(internos);
            }
            internos.Atualizar(dados.ProprietarioNome, dados.Endereco, autor, agora);
        }

        await _unitOfWork.Commit();
        await _cache.Invalidar();
        return imovel;
    }

    /// <summary>Envia uma foto e a inclui no fim da lista (vira capa se for a primeira).</summary>
    internal async Task<FotoAdicionada> AdicionarFoto(Guid imovelId, byte[] conteudo)
    {
        var imovel = await ExigirImovel(imovelId);
        var atuais = await _fotos.GetDoImovel(imovel.Id);
        if (atuais.Count >= PlanejamentoDeFotos.MaximoPorImovel)
            throw DomainException.Validacao($"O imóvel já tem {PlanejamentoDeFotos.MaximoPorImovel} fotos, que é o máximo.");

        var enviada = await _imagens.Gravar(DestinoImagem.DoImovel(imovel.Id), conteudo);
        var plano = PlanejamentoDeFotos.Planejar(atuais,
        [
            .. atuais.Select(f => new FotoDesejada(f.Id, f.Url, f.CaminhoNoStorage, f.Capa)),
            new FotoDesejada(null, enviada.Url, enviada.CaminhoNoStorage, false),
        ]);
        AplicarPlano(imovel.Id, atuais, plano);
        await _unitOfWork.Commit();
        await _cache.Invalidar();
        return new FotoAdicionada(enviada, plano.Fotos.Count);
    }

    internal async Task<string?> NomeDoBairro(Guid? bairroId)
    {
        if (bairroId is not { } id) return null;
        return (await _bairros.GetById(id))?.Nome
            ?? throw DomainException.Validacao("Bairro não encontrado. Escolha um bairro da lista.");
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<Imovel> ExigirImovel(Guid id) =>
        await _imoveis.GetById(id) ?? throw new DomainException(CodigoErro.NaoEncontrado, "Imóvel não encontrado.");

    /// <summary>Remove, reposiciona e inclui as fotos conforme o plano. Devolve a lista final, na ordem.</summary>
    private List<FotoImovel> AplicarPlano(Guid imovelId, IReadOnlyList<FotoImovel> atuais, PlanoDeFotos plano)
    {
        foreach (var removida in plano.Removidas) _fotos.Delete(removida);
        var porId = atuais.ToDictionary(f => f.Id);
        List<FotoImovel> finais = [];
        foreach (var f in plano.Fotos)
        {
            if (f.Id is { } id)
            {
                porId[id].Posicionar(f.Ordem, f.Capa);
                finais.Add(porId[id]);
            }
            else
            {
                var nova = new FotoImovel(imovelId, f.Url, f.CaminhoNoStorage, f.Ordem, f.Capa);
                _fotos.Add(nova);
                finais.Add(nova);
            }
        }
        return finais;
    }

    /// <summary>Depois de gravar: apaga do storage os arquivos que nenhuma foto usa mais.</summary>
    private async Task ApagarArquivosSemUso(IEnumerable<string?> caminhos)
    {
        var candidatos = caminhos.OfType<string>().Where(c => c.Length > 0).Distinct().ToList();
        if (candidatos.Count == 0) return;
        var apagar = PlanejamentoDeFotos.ArquivosParaApagar(candidatos, await _fotos.CaminhosEmUso(candidatos));
        if (apagar.Count > 0) await _arquivos.Remover(apagar);
    }
}
