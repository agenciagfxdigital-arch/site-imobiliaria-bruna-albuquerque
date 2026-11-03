using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Site;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>
/// Imóveis no site público (sem login). A lista e a página do imóvel mostram o que está ativo ou com o selo de
/// vendido/alugado (<see cref="Imovel.EstaVisivelNoSite"/>); a home, os semelhantes e o sitemap, só os ativos.
/// Nada daqui leva proprietário ou endereço exato.
/// </summary>
public sealed class ImovelDoSiteService(
    IImovelDoSiteRepository imoveis,
    IFotoRepository fotos,
    IBairroRepository bairros,
    IConfiguracoesRepository configuracoes,
    IRelogio relogio)
{
    public const int LimiteDeDestaques = 9;
    public const int LimiteDeAlugueis = 10;
    public const int LimiteDoMcmv = 12;
    private const int CandidatosASemelhantes = 12;
    private const int LimiteDoSitemap = 5000;

    private readonly IImovelDoSiteRepository _imoveis = imoveis;
    private readonly IFotoRepository _fotos = fotos;
    private readonly IBairroRepository _bairros = bairros;
    private readonly IConfiguracoesRepository _configuracoes = configuracoes;
    private readonly IRelogio _relogio = relogio;

    /// <summary>Lista com filtros, 12 por página. Bairro que não existe (link antigo, digitado errado) não acha nada.</summary>
    public Task<ApiResponse<ListaDeImoveisResponse>> Pesquisar(FiltroDoSite filtro) =>
        Resposta.De(async () =>
        {
            var normalizado = filtro.Normalizado();
            var bairros = await _bairros.GetAll();
            Guid? bairroId = null;
            if (normalizado.Bairro is { } slug)
            {
                bairroId = bairros.FirstOrDefault(b => b.Slug == slug)?.Id;
                if (bairroId is null) return new ListaDeImoveisResponse([], 0, normalizado.Pagina, FiltroDoSite.ItensPorPagina);
            }

            var (itens, total) = await _imoveis.Pesquisar(normalizado, bairroId, _relogio.Agora, await DiasDoSelo());
            return new ListaDeImoveisResponse(await Cartoes(itens, bairros), total, normalizado.Pagina, FiltroDoSite.ItensPorPagina);
        });

    public Task<ApiResponse<IReadOnlyList<CartaoDeImovelResponse>>> Destaques() =>
        Lista(() => _imoveis.Destaques(LimiteDeDestaques));

    public Task<ApiResponse<IReadOnlyList<CartaoDeImovelResponse>>> AlugueisRecentes() =>
        Lista(() => _imoveis.Recentes(Finalidade.Aluguel, somenteMcmv: false, LimiteDeAlugueis));

    public Task<ApiResponse<IReadOnlyList<CartaoDeImovelResponse>>> DoMcmv() =>
        Lista(() => _imoveis.Recentes(finalidade: null, somenteMcmv: true, LimiteDoMcmv));

    /// <param name="trecho">Código (<c>BA101</c>) ou o trecho da URL (<c>ba101-apartamento-…</c>).</param>
    public Task<ApiResponse<PaginaDoImovelResponse>> Obter(string trecho) =>
        Resposta.De(async () =>
        {
            var imovel = Imovel.CodigoDoTrecho(trecho) is { } codigo
                ? await _imoveis.GetVisivelByCodigo(codigo, _relogio.Agora, await DiasDoSelo())
                : null;
            if (imovel is null) throw new DomainException(CodigoErro.NaoEncontrado, "Imóvel não encontrado.");

            var bairros = await _bairros.GetAll();
            var candidatos = await _imoveis.CandidatosASemelhantes(imovel, CandidatosASemelhantes);
            return new PaginaDoImovelResponse(
                ImovelDoSiteResponse.De(imovel, bairros.FirstOrDefault(b => b.Id == imovel.BairroId), await _fotos.GetDoImovel(imovel.Id)),
                await Cartoes(ImoveisSemelhantes.Escolher(imovel, candidatos), bairros));
        });

    public Task<ApiResponse<IReadOnlyList<ImovelNoSitemapResponse>>> Sitemap() =>
        Resposta.De(async () => (IReadOnlyList<ImovelNoSitemapResponse>)
        [
            .. (await _imoveis.ParaSitemap(LimiteDoSitemap))
                .Select(i => new ImovelNoSitemapResponse(Imovel.CaminhoNoSite(i.Codigo, i.Slug), i.AtualizadoEm)),
        ]);

    // ---------------------------------------------------------------------------------------------

    private Task<ApiResponse<IReadOnlyList<CartaoDeImovelResponse>>> Lista(Func<Task<IReadOnlyList<Imovel>>> consulta) =>
        Resposta.De(async () => await Cartoes(await consulta(), await _bairros.GetAll()));

    private async Task<IReadOnlyList<CartaoDeImovelResponse>> Cartoes(IReadOnlyList<Imovel> imoveis, IReadOnlyList<Bairro> bairros)
    {
        if (imoveis.Count == 0) return [];
        var capas = await _fotos.GetCapas([.. imoveis.Select(i => i.Id)]);
        var nomes = bairros.ToDictionary(b => b.Id, b => b.Nome);
        return
        [
            .. imoveis.Select(i => CartaoDeImovelResponse.De(
                i, i.BairroId is { } bairro ? nomes.GetValueOrDefault(bairro) : null, capas.GetValueOrDefault(i.Id))),
        ];
    }

    /// <summary>Enquanto ninguém salvou as configurações, vale o padrão (30 dias).</summary>
    private async Task<int> DiasDoSelo() => (await _configuracoes.Get() ?? new Configuracoes()).DiasSeloVendido;
}
