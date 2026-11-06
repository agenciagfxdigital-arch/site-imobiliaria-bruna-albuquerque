using GFX.SiteBruna.Aplicacao.DTOs.Operadores;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>Quem pode usar o assistente do WhatsApp.</summary>
[Route("api/[controller]")]
public sealed class OperadorController(OperadorService operadores) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => RespostaParaStatus(await operadores.Listar());

    [HttpPost]
    public async Task<IActionResult> Salvar([FromBody] SalvarOperadorRequest request) => RespostaParaStatus(await operadores.Salvar(request));
}
