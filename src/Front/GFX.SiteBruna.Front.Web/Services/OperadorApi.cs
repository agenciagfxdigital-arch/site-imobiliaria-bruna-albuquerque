using GFX.SiteBruna.Front.Web.Autenticacao;
using GFX.SiteBruna.Front.Web.DTOs;
using GFX.SiteBruna.Front.Web.DTOs.Operadores;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>Telefones autorizados a usar o assistente do WhatsApp (<c>/api/Operador</c>).</summary>
public sealed class OperadorApi(HttpClient http, SessaoDoPainel sessao) : ApiClientBase(http, sessao)
{
    public Task<ApiResponse<List<OperadorResponse>>> Listar() => Get<List<OperadorResponse>>("api/Operador");

    public Task<ApiResponse<OperadorResponse>> Salvar(SalvarOperadorRequest request) => Post<OperadorResponse>("api/Operador", request);
}
