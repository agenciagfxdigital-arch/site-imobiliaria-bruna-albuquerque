using System.Net.Http.Headers;
using System.Net.Http.Json;
using GFX.SiteBruna.Aplicacao.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>Imagens públicas no Supabase Storage (bucket "media"), pela API REST com a chave de serviço.</summary>
internal sealed class ArmazenamentoSupabase(HttpClient http, IOptions<OpcoesDoSupabase> opcoes, ILogger<ArmazenamentoSupabase> logger)
    : IArmazenamentoDeArquivos
{
    private readonly OpcoesDoSupabase _opcoes = opcoes.Value;

    public async Task<string> Enviar(string caminho, byte[] conteudo, string tipo)
    {
        using var requisicao = Requisicao(HttpMethod.Post, $"storage/v1/object/{_opcoes.Bucket}/{caminho}");
        requisicao.Content = new ByteArrayContent(conteudo);
        requisicao.Content.Headers.ContentType = new MediaTypeHeaderValue(tipo);
        // O nome do arquivo é único: pode ficar em cache por 1 ano
        requisicao.Headers.Add("cache-control", "max-age=31536000");
        requisicao.Headers.Add("x-upsert", "false");

        using var resposta = await http.SendAsync(requisicao);
        if (!resposta.IsSuccessStatusCode)
            throw new InvalidOperationException($"[storage] enviar {caminho}: HTTP {(int)resposta.StatusCode} {await Resumo(resposta)}");
        return $"{_opcoes.Url.TrimEnd('/')}/storage/v1/object/public/{_opcoes.Bucket}/{caminho}";
    }

    public async Task Remover(IReadOnlyCollection<string> caminhos)
    {
        if (caminhos.Count == 0) return;
        using var requisicao = Requisicao(HttpMethod.Delete, $"storage/v1/object/{_opcoes.Bucket}");
        requisicao.Content = JsonContent.Create(new { prefixes = caminhos });
        using var resposta = await http.SendAsync(requisicao);
        // Arquivo que sobrou no storage não quebra o site: registra e segue.
        if (!resposta.IsSuccessStatusCode)
            logger.LogWarning("[storage] remover {Quantos} arquivo(s): HTTP {Status} {Resumo}",
                caminhos.Count, (int)resposta.StatusCode, await Resumo(resposta));
    }

    private HttpRequestMessage Requisicao(HttpMethod metodo, string caminho)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.Url) || string.IsNullOrWhiteSpace(_opcoes.ChaveServico))
            throw new InvalidOperationException("Configure Supabase:Url e Supabase:ChaveServico para gravar imagens.");
        var requisicao = new HttpRequestMessage(metodo, $"{_opcoes.Url.TrimEnd('/')}/{caminho}");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opcoes.ChaveServico);
        requisicao.Headers.Add("apikey", _opcoes.ChaveServico);
        return requisicao;
    }

    private static async Task<string> Resumo(HttpResponseMessage resposta)
    {
        var texto = await resposta.Content.ReadAsStringAsync();
        return texto.Length > 300 ? texto[..300] : texto;
    }
}
