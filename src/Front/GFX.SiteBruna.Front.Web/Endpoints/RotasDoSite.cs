using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using GFX.SiteBruna.Front.Web.Cache;
using GFX.SiteBruna.Front.Web.Configuracao;
using GFX.SiteBruna.Front.Web.Services;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Front.Web.Endpoints;

/// <summary>Rotas do site que não são páginas: sitemap, robots, eventos de navegação e o aviso de conteúdo novo.</summary>
public static class RotasDoSite
{
    private const int TamanhoMaximoDoEvento = 4 * 1024;

    private static readonly string[] PaginasFixas =
        ["", "/imoveis", "/aluguel", "/minha-casa-minha-vida", "/sobre", "/contato", "/privacidade", "/termos"];

    public static void MapRotasDoSite(this WebApplication app)
    {
        app.MapGet("/robots.txt", (IOptions<OpcoesDoSite> opcoes) =>
            Results.Text($"""
                User-Agent: *
                Allow: /
                Disallow: /admin
                Disallow: /eventos
                Disallow: /_cache

                Host: {opcoes.Value.UrlBase}
                Sitemap: {opcoes.Value.UrlBase}/sitemap.xml

                """, "text/plain; charset=utf-8"));

        app.MapGet("/sitemap.xml", async (SiteApi api, IOptions<OpcoesDoSite> opcoes) =>
        {
            var imoveis = await DadosDoSite.Exigir(api.Sitemap());
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var url = (string caminho, string prioridade, DateTimeOffset? alterado) => new XElement(ns + "url",
                new XElement(ns + "loc", $"{opcoes.Value.UrlBase}{caminho}"),
                alterado is { } quando ? new XElement(ns + "lastmod", quando.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")) : null,
                new XElement(ns + "changefreq", "weekly"),
                new XElement(ns + "priority", prioridade));
            var documento = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(ns + "urlset",
                PaginasFixas.Select(p => url(p, p == "" ? "1" : "0.8", null)),
                imoveis.Select(i => url(i.Caminho, "0.7", i.AtualizadoEm))));
            return Results.Text(documento.Declaration + "\n" + documento.Root, "application/xml; charset=utf-8");
        }).CacheOutput(p => p.Expire(TimeSpan.FromHours(1)).Tag(CacheDoSite.Etiqueta));

        // O navegador manda aqui (mesmo endereço do site); o Front repassa à Api, que fica só na rede interna
        app.MapPost("/eventos", async (HttpContext http, SiteApi api) =>
        {
            var corpo = await LerAte(http.Request.Body, TamanhoMaximoDoEvento + 1, http.RequestAborted);
            if (corpo.Length is > 0 and <= TamanhoMaximoDoEvento)
                await api.RegistrarEvento(corpo, http.Request.Headers.UserAgent.ToString(),
                    http.Connection.RemoteIpAddress?.ToString(), http.RequestAborted);
            return Results.NoContent();
        });

        // A Api avisa que o conteúdo mudou (CacheDoSiteViaHttp): as páginas em cache são descartadas
        app.MapPost("/_cache/invalidar", async (HttpContext http, IOutputCacheStore cache, IOptions<OpcoesDoSite> opcoes) =>
        {
            var chave = opcoes.Value.ChaveDoCache;
            var recebida = http.Request.Headers["X-Chave-Cache"].ToString();
            if (string.IsNullOrEmpty(chave)
                || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(chave), Encoding.UTF8.GetBytes(recebida)))
                return Results.Unauthorized();
            await cache.EvictByTagAsync(CacheDoSite.Etiqueta, http.RequestAborted);
            return Results.NoContent();
        });
    }

    private static async Task<byte[]> LerAte(Stream corpo, int limite, CancellationToken ct)
    {
        var buffer = new byte[limite];
        var lidos = 0;
        int n;
        while (lidos < limite && (n = await corpo.ReadAsync(buffer.AsMemory(lidos, limite - lidos), ct)) > 0) lidos += n;
        return buffer[..lidos];
    }
}
