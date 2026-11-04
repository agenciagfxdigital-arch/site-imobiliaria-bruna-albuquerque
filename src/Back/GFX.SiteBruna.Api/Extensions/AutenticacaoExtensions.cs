using System.Text;
using GFX.SiteBruna.Aplicacao.Authorization;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Dados;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace GFX.SiteBruna.Api.Extensions;

public static class AutenticacaoExtensions
{
    public static IServiceCollection AddAutenticacaoDoPainel(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment ambiente)
    {
        var secao = configuration.GetSection(DependencyInjection.SecaoDoJwt);
        var chave = secao["Key"] ?? throw new InvalidOperationException(
            $"{DependencyInjection.SecaoDoJwt}:Key não configurada (ambiente '{ambiente.EnvironmentName}'). Em Development " +
            $"use os user secrets; nos demais ambientes, a variável de ambiente {DependencyInjection.SecaoDoJwt}__Key.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                opcoes.MapInboundClaims = false;
                opcoes.RequireHttpsMetadata = !ambiente.IsDevelopment();
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = secao["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = secao["Audience"],
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
