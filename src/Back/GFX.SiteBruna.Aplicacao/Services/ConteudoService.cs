using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Configurações do site e textos das páginas (linhas únicas). Toda escrita avisa o site para refazer o cache.</summary>
public sealed class ConteudoService(
    IConfiguracoesRepository configuracoes,
    IPaginasRepository paginas,
    ICacheDoSite cache,
    IUnitOfWork unitOfWork,
    IRelogio relogio)
{
    private readonly IConfiguracoesRepository _configuracoes = configuracoes;
    private readonly IPaginasRepository _paginas = paginas;
    private readonly ICacheDoSite _cache = cache;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;

    /// <summary>Enquanto ninguém salvou, devolve os valores padrão (WhatsApp e CRECI da Bruna, cidade, selo de 30 dias).</summary>
    public Task<ApiResponse<ConfiguracoesResponse>> Configuracoes() =>
        Resposta.De(async () => ConfiguracoesResponse.De(await _configuracoes.Get() ?? new Configuracoes()));

    public Task<ApiResponse<ConfiguracoesResponse>> SalvarConfiguracoes(DadosDasConfiguracoes dados) =>
        Resposta.De(async () =>
        {
            var c = await _configuracoes.Get();
            if (c is null)
            {
                c = new Configuracoes();
                _configuracoes.Add(c);
            }
            c.Atualizar(dados, _relogio.Agora);
            await GravarEAvisar();
            return ConfiguracoesResponse.De(c);
        });

    /// <summary>Enquanto uma página não foi salva, devolve os textos padrão dela.</summary>
    public Task<ApiResponse<PaginasResponse>> Paginas() =>
        Resposta.De(async () => PaginasResponse.De(await _paginas.Get() ?? new Paginas()));

    public Task<ApiResponse<bool>> SalvarHome(ConteudoHome conteudo) =>
        AlterarPaginas(p => p.AtualizarHome(conteudo, _relogio.Agora));

    public Task<ApiResponse<bool>> SalvarSobre(ConteudoSobre conteudo) =>
        AlterarPaginas(p => p.AtualizarSobre(conteudo, _relogio.Agora));

    public Task<ApiResponse<bool>> SalvarMcmv(ConteudoMcmv conteudo) =>
        AlterarPaginas(p => p.AtualizarMcmv(conteudo, _relogio.Agora));

    private Task<ApiResponse<bool>> AlterarPaginas(Action<Paginas> alterar) =>
        Resposta.De(async () =>
        {
            var p = await _paginas.Get();
            if (p is null)
            {
                p = new Paginas();
                _paginas.Add(p);
            }
            alterar(p);
            await GravarEAvisar();
        });

    private async Task GravarEAvisar()
    {
        await _unitOfWork.Commit();
        await _cache.Invalidar();
    }
}
