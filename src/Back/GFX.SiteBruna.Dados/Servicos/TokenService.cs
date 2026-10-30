using System.Security.Claims;
using System.Text;
using GFX.SiteBruna.Aplicacao.Authorization;
using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;
using GFX.SiteBruna.Aplicacao.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>
/// JWT do painel (HS256). A chave vem de <c>Jwt:Key</c> (user secrets em Development; variável <c>Jwt__Key</c> nos
/// demais ambientes) e nunca fica em arquivo versionado.
/// </summary>
internal sealed class TokenService : ITokenService
{
    private readonly SymmetricSecurityKey _chave;
    private readonly string _emissor;
    private readonly string _publico;
    private readonly int _minutos;
    private readonly IRelogio _relogio;

    public TokenService(IConfiguration configuration, IRelogio relogio)
    {
        var chave = configuration["Jwt:Key"] ?? throw new InvalidOperationException(
            "Configure Jwt:Key (user secrets em Development; variável de ambiente Jwt__Key nos demais).");
        if (Encoding.UTF8.GetByteCount(chave) < 32) throw new InvalidOperationException("Jwt:Key precisa ter no mínimo 32 bytes.");
        _chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave));
        _emissor = configuration["Jwt:Issuer"] ?? "";
        _publico = configuration["Jwt:Audience"] ?? "";
        _minutos = configuration.GetValue<int?>("Jwt:MinutosDoToken") ?? 480;
        _relogio = relogio;
    }

    public (string Token, DateTimeOffset ExpiraEm) Gerar(UsuarioResponse usuario)
    {
        var expiraEm = _relogio.Agora.AddMinutes(_minutos);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimsUsuario.Id, usuario.Id.ToString("D")),
                new Claim(ClaimsUsuario.Nome, usuario.Nome),
                new Claim(ClaimsUsuario.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            Expires = expiraEm.UtcDateTime,
            Issuer = _emissor,
            Audience = _publico,
            SigningCredentials = new SigningCredentials(_chave, SecurityAlgorithms.HmacSha256),
        });
        return (token, expiraEm);
    }
}
