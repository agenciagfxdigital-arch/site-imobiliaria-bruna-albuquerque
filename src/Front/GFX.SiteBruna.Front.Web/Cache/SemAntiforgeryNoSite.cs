using Microsoft.Extensions.Primitives;

namespace GFX.SiteBruna.Front.Web.Cache;

/// <summary>
/// O Blazor grava o cookie de antiforgery (e "no-store") em toda página estática. O site público não tem formulário POST
/// (os filtros são GET), então o cookie sai das respostas dele, inclusive das que vêm do cache; o painel (/admin) e o
/// circuito do Blazor continuam com ele. Precisa vir antes do <c>UseOutputCache</c>, para valer também na página guardada
/// (o <see cref="CacheDoSite"/> não deixa de guardar por causa deste cookie).
/// </summary>
internal static class SemAntiforgeryNoSite
{
    public static IApplicationBuilder UseSemAntiforgeryNoSite(this IApplicationBuilder app) => app.Use((contexto, proximo) =>
    {
        var requisicao = contexto.Request;
        if (HttpMethods.IsGet(requisicao.Method) && !requisicao.Path.StartsWithSegments("/admin")
            && !requisicao.Path.StartsWithSegments("/_blazor"))
        {
            contexto.Response.OnStarting(() =>
            {
                var cabecalhos = contexto.Response.Headers;
                var cookies = cabecalhos.SetCookie.Where(c => c is not null && !EhDoAntiforgery(c)).ToArray();
                if (cookies.Length == cabecalhos.SetCookie.Count) return Task.CompletedTask;
                cabecalhos.SetCookie = new StringValues(cookies);
                cabecalhos.Remove("Pragma");
                cabecalhos.CacheControl = "no-cache";
                return Task.CompletedTask;
            });
        }
        return proximo(contexto);
    });

    public static bool EhDoAntiforgery(string cookie) => cookie.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal);
}
