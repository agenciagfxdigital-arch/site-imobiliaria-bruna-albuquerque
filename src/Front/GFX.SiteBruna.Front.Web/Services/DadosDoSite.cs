using GFX.SiteBruna.Front.Web.DTOs;
using GFX.SiteBruna.Front.Web.DTOs.Conteudo;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>
/// O que o layout e a página usam juntos (configurações, textos, bairros): uma chamada à Api por requisição.
/// Sem a Api, as configurações caem no padrão (o WhatsApp continua certo); o resto vira a página de erro.
/// </summary>
public sealed class DadosDoSite(SiteApi api, ILogger<DadosDoSite> logger)
{
    private Task<ConfiguracoesResponse>? _configuracoes;
    private Task<PaginasResponse>? _paginas;
    private Task<List<BairroResponse>>? _bairros;

    public Task<ConfiguracoesResponse> Configuracoes() => _configuracoes ??= CarregarConfiguracoes();

    public Task<PaginasResponse> Paginas() => _paginas ??= Exigir(api.Paginas());

    public Task<List<BairroResponse>> Bairros() => _bairros ??= Exigir(api.Bairros());

    private async Task<ConfiguracoesResponse> CarregarConfiguracoes()
    {
        var resposta = await api.Configuracoes();
        if (resposta is { Sucesso: true, Dados: { } dados }) return dados;
        logger.LogWarning("[site] configurações indisponíveis ({Mensagem}); usando o padrão", string.Join(" ", resposta.Mensagens));
        return ConfiguracoesResponse.Padrao();
    }

    /// <summary>Dado sem o qual a página não faz sentido: falha vira a página de erro (e não entra no cache).</summary>
    public static async Task<T> Exigir<T>(Task<ApiResponse<T>> chamada)
    {
        var resposta = await chamada;
        return resposta is { Sucesso: true, Dados: { } dados }
            ? dados
            : throw new SiteIndisponivelException(string.Join(" ", resposta.Mensagens));
    }
}
