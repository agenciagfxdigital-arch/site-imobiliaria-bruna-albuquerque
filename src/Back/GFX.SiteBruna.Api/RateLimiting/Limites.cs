using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GFX.SiteBruna.Api.RateLimiting;

/// <summary>
/// Cotas por origem (IP). Atrás do Caddy/Front, o IP real vem do <c>X-Forwarded-For</c>
/// (<c>ASPNETCORE_FORWARDEDHEADERS_ENABLED=true</c>; seguro porque só o proxy alcança a Api).
/// </summary>
public static class Limites
{
    public const string Login = "login";
    public const string Eventos = "eventos";
    public const string Webhook = "webhook";

    public static IServiceCollection AddLimites(this IServiceCollection services) =>
        services.AddRateLimiter(opcoes =>
        {
            Politica(opcoes, Login, 10);     // força bruta na senha
            Politica(opcoes, Eventos, 120);  // métricas do site
            Politica(opcoes, Webhook, 600);  // a Meta manda vários por segundo num lote de fotos
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

    private static void Politica(RateLimiterOptions opcoes, string nome, int porMinuto) =>
        opcoes.AddPolicy(nome, contexto => RateLimitPartition.GetFixedWindowLimiter(
            $"{nome}:{contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = porMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
}
