using GFX.SiteBruna.Api.RateLimiting;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>
/// Leituras do site público (sem login) e registro de eventos de navegação. Nada aqui devolve dado interno
/// (proprietário, endereço exato). Os imóveis públicos entram com o site (semana 3).
/// </summary>
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class SiteController(
    ConteudoService conteudo, FaqService faqs, DepoimentoService depoimentos, BairroService bairros, EventoService eventos)
    : ApiControllerBase
{
    [HttpGet("configuracoes")]
    public async Task<IActionResult> Configuracoes() => RespostaParaStatus(await conteudo.Configuracoes());

    [HttpGet("paginas")]
    public async Task<IActionResult> Paginas() => RespostaParaStatus(await conteudo.Paginas());

    [HttpGet("faqs")]
    public async Task<IActionResult> Faqs() => RespostaParaStatus(await faqs.Listar());

    [HttpGet("depoimentos")]
    public async Task<IActionResult> Depoimentos([FromQuery] bool home = false) => RespostaParaStatus(await depoimentos.Listar(home));

    [HttpGet("bairros")]
    public async Task<IActionResult> Bairros() => RespostaParaStatus(await bairros.Listar());

    /// <summary>Sempre 204: o visitante não precisa saber se o evento foi gravado ou ignorado.</summary>
    [HttpPost("eventos")]
    [EnableRateLimiting(Limites.Eventos)]
    [RequestSizeLimit(4 * 1024)]
    public async Task<IActionResult> Evento([FromBody] DadosDoEvento evento)
    {
        await eventos.Registrar(evento, Request.Headers.UserAgent.ToString());
        return NoContent();
    }
}
