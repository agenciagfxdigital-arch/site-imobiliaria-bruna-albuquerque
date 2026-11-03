namespace GFX.SiteBruna.Front.Web.DTOs.Usuarios;

public sealed record SessaoResponse(string Token, DateTimeOffset ExpiraEm, UsuarioResponse Usuario);
