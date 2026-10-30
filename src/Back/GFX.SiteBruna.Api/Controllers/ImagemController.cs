using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Modelo.Regras;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>
/// Envio de imagens do painel. O formato é conferido pelos bytes e a imagem é regravada em WebP sem EXIF/GPS
/// (nunca vai direto para o storage).
/// </summary>
[Route("api/[controller]")]
public sealed class ImagemController(ImagemService imagens) : ApiControllerBase
{
    /// <param name="pasta">"imoveis/&lt;id&gt;", "site", "depoimentos" ou "entregas".</param>
    [HttpPost]
    [RequestSizeLimit(Imagens.TamanhoMaximoBytes + 1024 * 1024)]
    public async Task<IActionResult> Enviar([FromForm] string pasta, IFormFile arquivo)
    {
        await using var fluxo = arquivo.OpenReadStream();
        using var memoria = new MemoryStream();
        await fluxo.CopyToAsync(memoria);
        return RespostaParaStatus(await imagens.Enviar(pasta, memoria.ToArray()));
    }
}
