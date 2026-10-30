using GFX.SiteBruna.Aplicacao.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>
/// Avisa o Front.Web (pela rede interna) que o conteúdo mudou, para refazer as páginas em cache. Se o aviso falhar, o
/// site mostra o conteúdo novo quando o cache vencer sozinho (alguns minutos): registra e segue.
/// </summary>
internal sealed class CacheDoSiteViaHttp(HttpClient http, IOptions<OpcoesDoSite> opcoes, ILogger<CacheDoSiteViaHttp> logger)
    : ICacheDoSite
{
    public const string Rota = "_cache/invalidar";
    public const string Cabecalho = "X-Chave-Cache";

    private readonly OpcoesDoSite _opcoes = opcoes.Value;

    public async Task Invalidar()
    {
        if (string.IsNullOrWhiteSpace(_opcoes.UrlInternaDoFront) || string.IsNullOrWhiteSpace(_opcoes.ChaveDoCache)) return;
        try
        {
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, $"{_opcoes.UrlInternaDoFront.TrimEnd('/')}/{Rota}");
            requisicao.Headers.Add(Cabecalho, _opcoes.ChaveDoCache);
            using var resposta = await http.SendAsync(requisicao);
            if (!resposta.IsSuccessStatusCode)
                logger.LogWarning("[cache] o front respondeu HTTP {Status} ao pedido de invalidação", (int)resposta.StatusCode);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "[cache] não consegui avisar o front");
        }
    }
}
