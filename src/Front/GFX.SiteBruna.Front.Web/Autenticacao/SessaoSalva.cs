using GFX.SiteBruna.Front.Web.DTOs.Usuarios;

namespace GFX.SiteBruna.Front.Web.Autenticacao;

public sealed record SessaoSalva(string Token, DateTimeOffset ExpiraEm, UsuarioResponse Usuario)
{
    public bool Expirada => ExpiraEm <= DateTimeOffset.UtcNow;

    public static SessaoSalva De(SessaoResponse resposta) => new(resposta.Token, resposta.ExpiraEm, resposta.Usuario);
}
