using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.ExceptionHandling;

/// <summary>Erro inesperado: registra no log e responde em português, sem detalhe técnico fora de Development.</summary>
public sealed class GlobalExceptionHandler(IHostEnvironment ambiente, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception erro, CancellationToken ct)
    {
        logger.LogError(erro, "Erro não tratado em {Caminho}", contexto.Request.Path);
        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Não consegui concluir agora. Tente de novo em alguns instantes.",
            Detail = ambiente.IsDevelopment() ? erro.ToString() : null,
        };
        contexto.Response.StatusCode = problema.Status.Value;
        await contexto.Response.WriteAsJsonAsync(problema, ct);
        return true;
    }
}
