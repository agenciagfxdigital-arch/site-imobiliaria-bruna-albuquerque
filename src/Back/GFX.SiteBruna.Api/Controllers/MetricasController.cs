using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>Números do início do painel.</summary>
[Route("api/[controller]")]
public sealed class MetricasController(MetricasService metricas) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter([FromQuery] int? dias, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate) =>
        RespostaParaStatus(await metricas.Obter(dias, de, ate));
}
