using System.Text;
using GFX.SiteBruna.Aplicacao.Authorization;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace GFX.SiteBruna.Api.Extensions;

public static class AutenticacaoExtensions
{
    public static IServiceCollection AddAutenticacaoDoPainel(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment ambiente)
    {
        var chave = configuration["Jwt:Key"] ?? throw new InvalidOperationException(
            $"Jwt:Key não configurada (ambiente '{ambiente.EnvironmentName}'). Em Development use os user secrets; " +
            "nos demais ambientes, a variável de ambiente Jwt__Key.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                opcoes.MapInboundClaims = false;
                opcoes.RequireHttpsMetadata = !ambiente.IsDevelopment();
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimsUsuario.Nome,
                };
                // O token vale algumas horas, mas quem foi desativado perde o acesso na hora.
                opcoes.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async contexto =>
                    {
                        var id = contexto.Principal?.FindFirst(ClaimsUsuario.Id)?.Value;
                        var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<UsuarioService>();
                        if (!Guid.TryParse(id, out var usuarioId) || !(await usuarios.Ativo(usuarioId)).Sucesso)
                            contexto.Fail("Usuário desativado ou excluído.");
                    },
                };
            });

        // Negação por padrão: rota nova nasce protegida; [AllowAnonymous] é a exceção explícita (site público e webhook).
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }
}
