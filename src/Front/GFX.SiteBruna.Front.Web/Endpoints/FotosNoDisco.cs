using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace GFX.SiteBruna.Front.Web.Endpoints;

/// <summary>Serve em <c>/media</c> as fotos que a Api grava na pasta <c>Fotos:Pasta</c> (na VPS, um volume nos dois contêineres).</summary>
internal static class FotosNoDisco
{
    public static IApplicationBuilder UseFotosNoDisco(this WebApplication app)
    {
        var pasta = app.Configuration["Fotos:Pasta"] is { Length: > 0 } texto
            ? Path.GetFullPath(texto, app.Environment.ContentRootPath)
            : throw new InvalidOperationException("Configure Fotos:Pasta com a mesma pasta da Api (docs/CONFIGURACAO.md).");
        Directory.CreateDirectory(pasta);

        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(pasta),
            RequestPath = "/media",
            // O nome do arquivo é único: pode ficar em cache por 1 ano
            OnPrepareResponse = c => c.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable",
        });
    }
}
