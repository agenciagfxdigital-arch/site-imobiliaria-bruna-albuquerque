using GFX.SiteBruna.Api.RateLimiting;
using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GFX.SiteBruna.Api.Controllers;

[Route("api/[controller]")]
public sealed class AuthController(UsuarioService usuarios) : ApiControllerBase
{
    [HttpPost("entrar")]
    [AllowAnonymous]
    [EnableRateLimiting(Limites.Login)]
    public async Task<IActionResult> Entrar([FromBody] EntrarRequest request) => RespostaParaStatus(await usuarios.Entrar(request));

    [HttpGet("eu")]
    public async Task<IActionResult> Eu() => RespostaParaStatus(await usuarios.Ativo(UsuarioId));

    [HttpPost("trocar-senha")]
    [EnableRateLimiting(Limites.Login)]
    public async Task<IActionResult> TrocarSenha([FromBody] TrocarSenhaRequest request) =>
        RespostaParaStatus(await usuarios.TrocarSenha(UsuarioId, request));
}
