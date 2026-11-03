using GFX.SiteBruna.Front.Web.Configuracao;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Front.Web.Cache;

/// <summary>
/// Cache das páginas do site (no lugar do ISR do Next): a página fica pronta por alguns minutos, separada por endereço e
/// filtros. Toda escrita no painel ou pelo WhatsApp avisa (<c>POST /_cache/invalidar</c>) e o cache é refeito.
/// O painel (<c>/admin</c>) nunca entra; só respostas 200 sem cookie (além do de antiforgery) são guardadas.
/// </summary>
internal sealed class CacheDoSite(IOptions<OpcoesDoSite> opcoes) : IOutputCachePolicy
{
    public const string Etiqueta = "site";

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var requisicao = context.HttpContext.Request;
        var minutos = opcoes.Value.CacheEmMinutos;
        var guardar = minutos > 0
            && (HttpMethods.IsGet(requisicao.Method) || HttpMethods.IsHead(requisicao.Method))
            && !requisicao.Path.StartsWithSegments("/admin");

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = guardar;
        context.AllowCacheStorage = guardar;
        context.AllowLocking = true;
        context.CacheVaryByRules.QueryKeys = "*";
        context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(Math.Max(minutos, 1));
        context.Tags.Add(Etiqueta);
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation) => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var resposta = context.HttpContext.Response;
        // O cookie de antiforgery sai na hora de responder (SemAntiforgeryNoSite); qualquer outro impede guardar
        if (resposta.StatusCode != StatusCodes.Status200OK
            || resposta.Headers.SetCookie.Any(c => c is not null && !SemAntiforgeryNoSite.EhDoAntiforgery(c)))
            context.AllowCacheStorage = false;
        return ValueTask.CompletedTask;
    }
}
