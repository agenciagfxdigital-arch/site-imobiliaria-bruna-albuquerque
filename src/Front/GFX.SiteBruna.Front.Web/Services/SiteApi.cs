using System.Globalization;
using GFX.SiteBruna.Front.Web.DTOs;
using GFX.SiteBruna.Front.Web.DTOs.Conteudo;
using GFX.SiteBruna.Front.Web.DTOs.Site;
using GFX.SiteBruna.Front.Web.Apresentacao;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>Rotas públicas da Api (<c>/api/Site</c>), sem login. Nada daqui traz dado interno do imóvel.</summary>
public sealed class SiteApi(HttpClient http) : ApiClientBase(http)
{
    public Task<ApiResponse<ConfiguracoesResponse>> Configuracoes() => Get<ConfiguracoesResponse>("api/Site/configuracoes", autenticada: false);

    public Task<ApiResponse<PaginasResponse>> Paginas() => Get<PaginasResponse>("api/Site/paginas", autenticada: false);

    public Task<ApiResponse<List<FaqResponse>>> Faqs() => Get<List<FaqResponse>>("api/Site/faqs", autenticada: false);

    public Task<ApiResponse<List<DepoimentoResponse>>> Depoimentos(bool soDaHome) =>
        Get<List<DepoimentoResponse>>($"api/Site/depoimentos?home={(soDaHome ? "true" : "false")}", autenticada: false);

    public Task<ApiResponse<List<BairroResponse>>> Bairros() => Get<List<BairroResponse>>("api/Site/bairros", autenticada: false);

    public Task<ApiResponse<ListaDeImoveisResponse>> Imoveis(FiltroDoSite filtro) =>
        Get<ListaDeImoveisResponse>($"api/Site/imoveis{ParaApi(filtro)}", autenticada: false);

    public Task<ApiResponse<List<CartaoDeImovelResponse>>> Destaques() =>
        Get<List<CartaoDeImovelResponse>>("api/Site/imoveis/destaques", autenticada: false);

    public Task<ApiResponse<List<CartaoDeImovelResponse>>> AlugueisRecentes() =>
        Get<List<CartaoDeImovelResponse>>("api/Site/imoveis/aluguel", autenticada: false);

    public Task<ApiResponse<List<CartaoDeImovelResponse>>> DoMcmv() =>
        Get<List<CartaoDeImovelResponse>>("api/Site/imoveis/mcmv", autenticada: false);

    /// <param name="trecho">O que vem depois de <c>/imovel/</c> na URL (a Api tira o código dali).</param>
    public Task<ApiResponse<PaginaDoImovelResponse>> Imovel(string trecho) =>
        Get<PaginaDoImovelResponse>($"api/Site/imovel/{Uri.EscapeDataString(trecho)}", autenticada: false);

    public Task<ApiResponse<List<ImovelNoSitemapResponse>>> Sitemap() =>
        Get<List<ImovelNoSitemapResponse>>("api/Site/sitemap", autenticada: false);

    /// <summary>Repassa um evento de navegação (a Api responde sempre 204).</summary>
    public async Task RegistrarEvento(byte[] corpo, string? userAgent, string? ip, CancellationToken ct)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "api/Site/eventos")
        {
            Content = new ByteArrayContent(corpo) { Headers = { ContentType = new("application/json") } },
        };
        if (!string.IsNullOrEmpty(userAgent)) requisicao.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        if (!string.IsNullOrEmpty(ip)) requisicao.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
        try
        {
            using var _ = await Http.SendAsync(requisicao, ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            // Métrica nunca derruba nada
        }
    }

    /// <summary>Filtro como a Api entende (nomes das propriedades do <c>FiltroDoSite</c> do Modelo).</summary>
    private static string ParaApi(FiltroDoSite f)
    {
        var partes = new List<string> { $"finalidade={f.Finalidade}" };
        void Incluir(string nome, object? valor)
        {
            if (valor is not null) partes.Add($"{nome}={Uri.EscapeDataString(Convert.ToString(valor, CultureInfo.InvariantCulture)!)}");
        }
        Incluir("tipo", f.Tipo);
        Incluir("bairro", f.Bairro);
        Incluir("precoMinimo", f.PrecoMinimo);
        Incluir("precoMaximo", f.PrecoMaximo);
        Incluir("quartos", f.Quartos);
        Incluir("vagas", f.Vagas);
        Incluir("situacao", f.Situacao);
        if (f.Mcmv) Incluir("mcmv", "true");
        if (f.EntradaParcelada) Incluir("entradaParcelada", "true");
        if (f.AceitaFgts) Incluir("aceitaFgts", "true");
        if (f.Mobiliado) Incluir("mobiliado", "true");
        if (f.AceitaPet) Incluir("aceitaPet", "true");
        Incluir("ordem", f.Ordem);
        Incluir("pagina", f.Pagina);
        return "?" + string.Join('&', partes);
    }
}
