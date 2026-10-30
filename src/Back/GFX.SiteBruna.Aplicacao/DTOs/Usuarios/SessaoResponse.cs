namespace GFX.SiteBruna.Aplicacao.DTOs.Usuarios;

public sealed record SessaoResponse(string Token, DateTimeOffset ExpiraEm, UsuarioResponse Usuario);
