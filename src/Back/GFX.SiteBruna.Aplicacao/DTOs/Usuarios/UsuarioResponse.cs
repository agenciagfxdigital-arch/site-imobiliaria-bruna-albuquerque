using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Usuarios;

public sealed record UsuarioResponse(Guid Id, string Nome, string Email, bool Ativo, DateTimeOffset? UltimoAcessoEm)
{
    public static UsuarioResponse De(Usuario u) => new(u.Id, u.Nome, u.Email, u.Ativo, u.UltimoAcessoEm);
}
