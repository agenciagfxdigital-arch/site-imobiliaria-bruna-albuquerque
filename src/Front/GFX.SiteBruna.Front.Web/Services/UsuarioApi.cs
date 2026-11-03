using GFX.SiteBruna.Front.Web.Autenticacao;
using GFX.SiteBruna.Front.Web.DTOs;
using GFX.SiteBruna.Front.Web.DTOs.Usuarios;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>
/// Login e usuários do painel (<c>/api/Auth</c>, <c>/api/Usuario</c>). Leva o IP de quem usa: entrar e trocar a senha têm
/// limite de tentativas por IP na Api.
/// </summary>
public sealed class UsuarioApi(HttpClient http, SessaoDoPainel sessao, OrigemDaConexao origem) : ApiClientBase(http, sessao, origem)
{
    /// <summary>Anônima: um 401 aqui é e-mail ou senha errados, não sessão vencida.</summary>
    public Task<ApiResponse<SessaoResponse>> Entrar(EntrarRequest request) =>
        Post<SessaoResponse>("api/Auth/entrar", request, autenticada: false);

    public Task<ApiResponse<UsuarioResponse>> Eu() => Get<UsuarioResponse>("api/Auth/eu");

    public Task<ApiResponse<bool>> TrocarSenha(TrocarSenhaRequest request) => Post<bool>("api/Auth/trocar-senha", request);

    public Task<ApiResponse<List<UsuarioResponse>>> Listar() => Get<List<UsuarioResponse>>("api/Usuario");

    public Task<ApiResponse<UsuarioResponse>> Criar(CriarUsuarioRequest request) => Post<UsuarioResponse>("api/Usuario", request);
}
