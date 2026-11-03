namespace GFX.SiteBruna.Front.Web.DTOs.Usuarios;

public sealed record UsuarioResponse(Guid Id, string Nome, string Email, bool Ativo, DateTimeOffset? UltimoAcessoEm);
