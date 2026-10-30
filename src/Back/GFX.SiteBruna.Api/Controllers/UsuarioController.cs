using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>Usuários do painel (todo usuário é administrador do site).</summary>
[Route("api/[controller]")]
public sealed class UsuarioController(UsuarioService usuarios) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => RespostaParaStatus(await usuarios.Listar());

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest request) => RespostaParaStatus(await usuarios.Criar(request));
}
