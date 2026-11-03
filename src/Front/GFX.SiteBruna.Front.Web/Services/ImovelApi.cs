using GFX.SiteBruna.Front.Web.Autenticacao;
using GFX.SiteBruna.Front.Web.DTOs;
using GFX.SiteBruna.Front.Web.DTOs.Imoveis;
using GFX.SiteBruna.Front.Web.DTOs.Site;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>Imóveis no painel (<c>/api/Imovel</c>), com os dados internos (proprietário, endereço exato).</summary>
public sealed class ImovelApi(HttpClient http, SessaoDoPainel sessao) : ApiClientBase(http, sessao)
{
    public Task<ApiResponse<List<ImovelResumoResponse>>> Listar(string? busca, StatusImovel? status, Finalidade? finalidade)
    {
        List<string> filtros = [];
        if (!string.IsNullOrWhiteSpace(busca)) filtros.Add($"busca={Uri.EscapeDataString(busca.Trim())}");
        if (status is { } s) filtros.Add($"status={s}");
        if (finalidade is { } f) filtros.Add($"finalidade={f}");
        return Get<List<ImovelResumoResponse>>("api/Imovel" + (filtros.Count > 0 ? "?" + string.Join('&', filtros) : ""));
    }

    public Task<ApiResponse<ImovelDetalheResponse>> Obter(Guid id) => Get<ImovelDetalheResponse>($"api/Imovel/{id}");

    /// <summary>A página como fica no site, em qualquer status.</summary>
    public Task<ApiResponse<ImovelDoSiteResponse>> Previa(Guid id) => Get<ImovelDoSiteResponse>($"api/Imovel/{id}/previa");

    public Task<ApiResponse<ImovelSalvoResponse>> Salvar(SalvarImovelRequest request) => Post<ImovelSalvoResponse>("api/Imovel", request);

    public Task<ApiResponse<List<FotoResponse>>> SalvarFotos(Guid id, IReadOnlyList<FotoDesejada> fotos) =>
        Put<List<FotoResponse>>($"api/Imovel/{id}/fotos", fotos);

    public Task<ApiResponse<bool>> AlterarStatus(Guid id, StatusImovel status) => Put<bool>($"api/Imovel/{id}/status", status);

    public Task<ApiResponse<bool>> AlterarDestaque(Guid id, bool destaque) => Put<bool>($"api/Imovel/{id}/destaque", destaque);

    public Task<ApiResponse<ImovelSalvoResponse>> Duplicar(Guid id) => Post<ImovelSalvoResponse>($"api/Imovel/{id}/duplicar", null);

    public Task<ApiResponse<bool>> Excluir(Guid id) => Delete<bool>($"api/Imovel/{id}");

    /// <summary>Foto do imóvel: entra na lista quando as fotos forem salvas.</summary>
    public Task<ApiResponse<ImagemEnviadaResponse>> EnviarFoto(Guid id, byte[] conteudo, string nome) =>
        PostArquivo<ImagemEnviadaResponse>("api/Imagem", "arquivo", conteudo, nome, new Dictionary<string, string?> { ["pasta"] = $"imoveis/{id:D}" });
}
