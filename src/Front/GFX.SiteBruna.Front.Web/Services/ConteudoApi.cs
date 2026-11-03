using GFX.SiteBruna.Front.Web.Autenticacao;
using GFX.SiteBruna.Front.Web.DTOs;
using GFX.SiteBruna.Front.Web.DTOs.Conteudo;
using GFX.SiteBruna.Front.Web.DTOs.Imoveis;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>
/// Conteúdo do site no painel: configurações, páginas, perguntas frequentes, depoimentos, bairros e imagens do site.
/// As leituras usam as mesmas rotas públicas do site (o que o painel edita é o que o site mostra).
/// </summary>
public sealed class ConteudoApi(HttpClient http, SessaoDoPainel sessao) : ApiClientBase(http, sessao)
{
    public Task<ApiResponse<ConfiguracoesResponse>> Configuracoes() => Get<ConfiguracoesResponse>("api/Site/configuracoes", autenticada: false);

    public Task<ApiResponse<ConfiguracoesResponse>> SalvarConfiguracoes(ConfiguracoesResponse dados) =>
        Put<ConfiguracoesResponse>("api/Conteudo/configuracoes", dados);

    public Task<ApiResponse<PaginasResponse>> Paginas() => Get<PaginasResponse>("api/Site/paginas", autenticada: false);

    public Task<ApiResponse<bool>> SalvarHome(ConteudoHome pagina) => Put<bool>("api/Conteudo/paginas/home", pagina);

    public Task<ApiResponse<bool>> SalvarSobre(ConteudoSobre pagina) => Put<bool>("api/Conteudo/paginas/sobre", pagina);

    public Task<ApiResponse<bool>> SalvarMcmv(ConteudoMcmv pagina) => Put<bool>("api/Conteudo/paginas/mcmv", pagina);

    public Task<ApiResponse<List<FaqResponse>>> Faqs() => Get<List<FaqResponse>>("api/Site/faqs", autenticada: false);

    public Task<ApiResponse<FaqResponse>> SalvarFaq(SalvarFaqRequest request) => Post<FaqResponse>("api/Faq", request);

    public Task<ApiResponse<bool>> ReordenarFaqs(IReadOnlyList<Guid> ids) => Put<bool>("api/Faq/ordem", ids);

    public Task<ApiResponse<bool>> ExcluirFaq(Guid id) => Delete<bool>($"api/Faq/{id}");

    public Task<ApiResponse<List<DepoimentoResponse>>> Depoimentos() =>
        Get<List<DepoimentoResponse>>("api/Site/depoimentos?home=false", autenticada: false);

    public Task<ApiResponse<DepoimentoResponse>> SalvarDepoimento(SalvarDepoimentoRequest request) =>
        Post<DepoimentoResponse>("api/Depoimento", request);

    public Task<ApiResponse<bool>> ExcluirDepoimento(Guid id) => Delete<bool>($"api/Depoimento/{id}");

    public Task<ApiResponse<List<BairroResponse>>> Bairros() => Get<List<BairroResponse>>("api/Site/bairros", autenticada: false);

    public Task<ApiResponse<BairroResponse>> SalvarBairro(SalvarBairroRequest request) => Post<BairroResponse>("api/Bairro", request);

    public Task<ApiResponse<bool>> ExcluirBairro(Guid id) => Delete<bool>($"api/Bairro/{id}");

    /// <param name="pasta">"site" (fotos da Bruna), "depoimentos" ou "entregas".</param>
    public Task<ApiResponse<ImagemEnviadaResponse>> EnviarImagem(string pasta, byte[] conteudo, string nome) =>
        PostArquivo<ImagemEnviadaResponse>("api/Imagem", "arquivo", conteudo, nome, new Dictionary<string, string?> { ["pasta"] = pasta });
}
