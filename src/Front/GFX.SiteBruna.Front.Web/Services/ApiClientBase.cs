using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GFX.SiteBruna.Front.Web.Autenticacao;
using GFX.SiteBruna.Front.Web.DTOs;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>
/// Base dos clientes da Api: transforma qualquer resposta (ou a falta dela) num <see cref="ApiResponse{T}"/>, para as telas
/// lidarem com um formato só e nunca verem exceção de rede. Nas rotas do painel, anexa o token da sessão; um 401 encerra a
/// sessão (o layout do painel manda para o login).
/// <para>
/// O token é anexado aqui, e não num <c>DelegatingHandler</c>: no Blazor Server o handler vive num escopo de DI próprio e
/// não enxergaria a sessão do circuito (mesmo motivo do sistema interno).
/// </para>
/// </summary>
public abstract class ApiClientBase(HttpClient http, SessaoDoPainel? sessao = null, OrigemDaConexao? origem = null)
{
    private readonly HttpClient _http = http;
    private readonly SessaoDoPainel? _sessao = sessao;
    private readonly OrigemDaConexao? _origem = origem;

    protected HttpClient Http => _http;

    public const string MensagemSemConexao = "Não consegui falar com o servidor. Confira a internet e tente de novo.";
    public const string MensagemMuitasTentativas = "Muitas tentativas seguidas. Espere um minuto e tente de novo.";
    public const string MensagemErroServidor = "O servidor não conseguiu atender agora. Tente de novo em instantes.";
    public const string MensagemRespostaInvalida = "Resposta inesperada do servidor. Tente de novo.";
    public const string MensagemSessaoExpirada = "Sua sessão expirou. Entre de novo.";

    protected Task<ApiResponse<T>> Get<T>(string rota, bool autenticada = true, CancellationToken ct = default) =>
        Enviar<T>(() => new HttpRequestMessage(HttpMethod.Get, rota), autenticada, ct);

    protected Task<ApiResponse<T>> Post<T>(string rota, object? corpo, bool autenticada = true, CancellationToken ct = default) =>
        Enviar<T>(() => new HttpRequestMessage(HttpMethod.Post, rota) { Content = Json.Conteudo(corpo) }, autenticada, ct);

    protected Task<ApiResponse<T>> Put<T>(string rota, object? corpo, CancellationToken ct = default) =>
        Enviar<T>(() => new HttpRequestMessage(HttpMethod.Put, rota) { Content = Json.Conteudo(corpo) }, autenticada: true, ct);

    protected Task<ApiResponse<T>> Delete<T>(string rota, CancellationToken ct = default) =>
        Enviar<T>(() => new HttpRequestMessage(HttpMethod.Delete, rota), autenticada: true, ct);

    /// <summary>Envio de arquivo (multipart/form-data) com campos de texto opcionais.</summary>
    protected Task<ApiResponse<T>> PostArquivo<T>(
        string rota, string campo, byte[] conteudo, string nome, IReadOnlyDictionary<string, string?>? campos = null,
        CancellationToken ct = default) =>
        Enviar<T>(() =>
        {
            var formulario = new MultipartFormDataContent { { new ByteArrayContent(conteudo), campo, nome } };
            foreach (var (chave, valor) in campos ?? new Dictionary<string, string?>())
                if (!string.IsNullOrWhiteSpace(valor)) formulario.Add(new StringContent(valor), chave);
            return new HttpRequestMessage(HttpMethod.Post, rota) { Content = formulario };
        }, autenticada: true, ct);

    private async Task<ApiResponse<T>> Enviar<T>(Func<HttpRequestMessage> criar, bool autenticada, CancellationToken ct)
    {
        using var requisicao = criar();
        if (autenticada)
        {
            if (_sessao is null || await _sessao.ObterSessao() is not { } atual) return await EncerrarSessao<T>();
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", atual.Token);
        }
        // A Api só é alcançada por este servidor: sem o IP de quem usa, o limite de tentativas valeria para todos juntos
        if (_origem?.Ip is { } ip) requisicao.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);

        try
        {
            using var resposta = await _http.SendAsync(requisicao, ct);
            if (autenticada && resposta.StatusCode == HttpStatusCode.Unauthorized) return await EncerrarSessao<T>();
            return await LerEnvelope<T>(resposta, ct);
        }
        catch (HttpRequestException)
        {
            return ApiResponse<T>.Erro(MensagemSemConexao);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResponse<T>.Erro(MensagemSemConexao); // tempo limite do HttpClient
        }
    }

    private async Task<ApiResponse<T>> EncerrarSessao<T>()
    {
        if (_sessao is not null) await _sessao.Sair();
        return new ApiResponse<T> { Sucesso = false, Falha = "NaoAutorizado", Mensagens = [MensagemSessaoExpirada] };
    }

    private static async Task<ApiResponse<T>> LerEnvelope<T>(HttpResponseMessage resposta, CancellationToken ct)
    {
        // O limitador responde 429 sem corpo
        if (resposta.StatusCode == HttpStatusCode.TooManyRequests) return ApiResponse<T>.Erro(MensagemMuitasTentativas);
        if ((int)resposta.StatusCode >= 500) return ApiResponse<T>.Erro(MensagemErroServidor);

        ApiResponse<T>? envelope;
        try
        {
            envelope = await resposta.Content.ReadFromJsonAsync<ApiResponse<T>>(Json.Opcoes, ct);
        }
        catch (JsonException)
        {
            envelope = null;
        }

        if (resposta.IsSuccessStatusCode)
            return envelope is { Sucesso: true } ? envelope : ApiResponse<T>.Erro(MensagemRespostaInvalida);
        // Erro com envelope (regra de negócio, login) traz a mensagem da Api; um ProblemDetails cai no texto genérico
        if (envelope is { Mensagens.Count: > 0 })
        {
            envelope.Sucesso = false;
            return envelope;
        }
        return ApiResponse<T>.Erro(MensagemRespostaInvalida);
    }
}
