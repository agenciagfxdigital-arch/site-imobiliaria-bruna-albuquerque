namespace GFX.SiteBruna.Front.Web.Cache;

/// <summary>Os mesmos cabeçalhos do site em Next, em toda resposta. O painel (/admin) fica fora dos buscadores.</summary>
internal static class CabecalhosDeSeguranca
{
    public static IApplicationBuilder UseCabecalhosDeSeguranca(this IApplicationBuilder app) => app.Use((contexto, proximo) =>
    {
        var cabecalhos = contexto.Response.Headers;
        cabecalhos.XContentTypeOptions = "nosniff";
        cabecalhos.XFrameOptions = "SAMEORIGIN";
        cabecalhos["Referrer-Policy"] = "strict-origin-when-cross-origin";
        if (contexto.Request.Path.StartsWithSegments("/admin")) cabecalhos["X-Robots-Tag"] = "noindex, nofollow";
        return proximo(contexto);
    });
}
