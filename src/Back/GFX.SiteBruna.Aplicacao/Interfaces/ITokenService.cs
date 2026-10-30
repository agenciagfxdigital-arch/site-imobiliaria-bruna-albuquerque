using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Token de acesso do painel (JWT), implementado no Dados.</summary>
public interface ITokenService
{
    (string Token, DateTimeOffset ExpiraEm) Gerar(UsuarioResponse usuario);
}
