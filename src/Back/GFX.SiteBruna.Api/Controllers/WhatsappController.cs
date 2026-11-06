using System.Text.Json;
using GFX.SiteBruna.Api.RateLimiting;
using GFX.SiteBruna.Api.Whatsapp;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>
/// Webhook da WhatsApp Cloud API (assistente do painel). Única rota da Api que fica pública na internet além do site.
/// GET: verificação pela Meta. POST: confere a assinatura, grava na fila e responde 200 na hora.
/// </summary>
[Route("api/whatsapp/webhook")]
[AllowAnonymous]
[EnableRateLimiting(Limites.Webhook)]
public sealed class WhatsappController(
    IOptions<OpcoesDoWebhook> opcoes, AssistenteService assistente, FilaDeProcessamento fila, ISaidaDoWhatsappSimulado saida,
    IHostEnvironment ambiente, ILogger<WhatsappController> logger)
    : ControllerBase
{
    private readonly OpcoesDoWebhook _opcoes = opcoes.Value;

    [HttpGet]
    public IActionResult Verificar(
        [FromQuery(Name = "hub.mode")] string? modo,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? desafio)
    {
        if (!_opcoes.Habilitado) return NotFound();
        return modo == "subscribe" && _opcoes.VerifyToken.Length >= 16 && AssinaturaDoWebhook.Iguais(token ?? "", _opcoes.VerifyToken)
            ? Content(desafio ?? "", "text/plain")
            : StatusCode(StatusCodes.Status403Forbidden);
    }

    /// <summary>Só no modo simulado e fora de produção: o que teria sido enviado (usado pelo comando simular-whatsapp).</summary>
    [HttpGet("saida")]
    public IActionResult Saida() =>
        _opcoes.Habilitado && _opcoes.Modo == "simulado" && !ambiente.IsProduction() ? Ok(saida.Retirar()) : NotFound();

    [HttpPost]
    [RequestSizeLimit(1024 * 1024)]
    public async Task<IActionResult> Receber()
    {
        if (!_opcoes.Habilitado) return NotFound();
        if (_opcoes.Pendencias() is { Count: > 0 } pendencias)
        {
            logger.LogError("[whatsapp] configuração incompleta: {Pendencias}", string.Join(", ", pendencias));
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        using var memoria = new MemoryStream();
        await Request.Body.CopyToAsync(memoria);
        var corpo = memoria.ToArray();
        if (!AssinaturaDoWebhook.Valida(corpo, Request.Headers["X-Hub-Signature-256"], _opcoes.AppSecret)) return Unauthorized();

        JsonElement payload;
        try
        {
            payload = JsonDocument.Parse(corpo).RootElement;
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        var mensagens = LeitorDoWebhook.Mensagens(payload, _opcoes.Modo == "simulado" ? null : _opcoes.PhoneNumberId);
        if (mensagens.Count == 0) return Ok(); // status de entrega, leitura etc.

        // Se gravar falhar, a exceção vira 500 e a Meta reenvia (a fila descarta duplicatas pelo wamid).
        foreach (var telefone in await assistente.Receber(mensagens)) fila.Avisar(telefone);
        return Ok();
    }
}
