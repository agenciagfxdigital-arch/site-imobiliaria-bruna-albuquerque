using GFX.SiteBruna.Aplicacao.Authorization;
using GFX.SiteBruna.Aplicacao.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>A única lógica permitida no controller: traduzir o <see cref="TipoFalha"/> do serviço em status HTTP.</summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult RespostaParaStatus<T>(ApiResponse<T> resposta)
    {
        if (resposta.Sucesso) return Ok(resposta);
        return resposta.Falha switch
        {
            TipoFalha.NaoEncontrado => NotFound(resposta),
            TipoFalha.NaoAutorizado => Unauthorized(resposta),
            TipoFalha.Conflito => Conflict(resposta),
            TipoFalha.Limite => StatusCode(StatusCodes.Status429TooManyRequests, resposta),
            _ => BadRequest(resposta),
        };
    }

    /// <summary>Id do usuário do painel (o token já foi conferido no <c>OnTokenValidated</c>).</summary>
    protected Guid UsuarioId => Guid.Parse(User.FindFirst(ClaimsUsuario.Id)!.Value);
}
