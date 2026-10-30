using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>
/// WhatsApp Cloud API (Meta): envia texto e botões e baixa as fotos recebidas.
/// Limites do WhatsApp: texto até 4096 caracteres; corpo com botões até 1024; até 3 botões de 20.
/// </summary>
internal sealed class CanalMeta(HttpClient http, IOptions<OpcoesDoWhatsapp> opcoes) : ICanalWhatsapp
{
    public const int LimiteTexto = 4096;
    public const int LimiteCorpoComBotoes = 1024;

    private readonly OpcoesDoWhatsapp _opcoes = opcoes.Value;
    private string Base => $"https://graph.facebook.com/{_opcoes.VersaoGraph}";

    public async Task EnviarTexto(string telefone, string texto)
    {
        foreach (var parte in DividirTexto(texto))
            await Enviar(new { to = telefone, type = "text", text = new { body = parte, preview_url = false } });
    }

    public async Task EnviarBotoes(string telefone, string texto, IReadOnlyList<BotaoDoWhatsapp> botoes)
    {
        var corpo = texto;
        if (texto.Length > LimiteCorpoComBotoes)
        {
            await EnviarTexto(telefone, texto);
            corpo = "Confirma?";
        }
        await Enviar(new
        {
            to = telefone,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text = corpo },
                action = new
                {
                    buttons = botoes.Take(3).Select(b => new
                    {
                        type = "reply",
                        reply = new { id = b.Id, title = b.Titulo.Length > 20 ? b.Titulo[..20] : b.Titulo },
                    }),
                },
            },
        });
    }

    public async Task<byte[]> BaixarMidia(string midiaId)
    {
        const string naoBaixou = "Não consegui baixar a foto. Envie de novo.";
        using var infoRequisicao = Requisicao(HttpMethod.Get, $"{Base}/{Uri.EscapeDataString(midiaId)}");
        using var info = await http.SendAsync(infoRequisicao);
        if (!info.IsSuccessStatusCode) throw DomainException.Validacao(naoBaixou);
        var midia = await info.Content.ReadFromJsonAsync<InfoDaMidia>() ?? throw DomainException.Validacao(naoBaixou);
        if (string.IsNullOrEmpty(midia.Url)) throw DomainException.Validacao(naoBaixou);
        if (midia.TamanhoDoArquivo > Imagens.TamanhoMaximoBytes) throw DomainException.Validacao("A foto é grande demais (máximo de 15 MB).");
        if (midia.TipoMime is { } mime && !mime.StartsWith("image/")) throw DomainException.Validacao("Esse arquivo não é uma foto.");

        using var arquivoRequisicao = Requisicao(HttpMethod.Get, midia.Url);
        using var arquivo = await http.SendAsync(arquivoRequisicao);
        if (!arquivo.IsSuccessStatusCode) throw DomainException.Validacao(naoBaixou);
        // O formato real é conferido pelos bytes depois (Modelo), não pelo mime informado.
        return await arquivo.Content.ReadAsByteArrayAsync();
    }

    /// <summary>Divide textos longos em partes, preferindo quebrar entre parágrafos.</summary>
    public static IReadOnlyList<string> DividirTexto(string texto, int limite = LimiteTexto)
    {
        if (texto.Length <= limite) return [texto];
        List<string> partes = [];
        var resto = texto;
        while (resto.Length > limite)
        {
            var corte = Math.Max(Math.Max(resto.LastIndexOf("\n\n", limite, StringComparison.Ordinal),
                resto.LastIndexOf('\n', limite)), limite / 2);
            partes.Add(resto[..corte].TrimEnd());
            resto = resto[corte..].TrimStart();
        }
        if (resto.Length > 0) partes.Add(resto);
        return partes;
    }

    private async Task Enviar(object corpo)
    {
        var payload = JsonSerializer.SerializeToNode(corpo)!.AsObject();
        payload["messaging_product"] = "whatsapp";
        payload["recipient_type"] = "individual";
        using var requisicao = Requisicao(HttpMethod.Post, $"{Base}/{_opcoes.PhoneNumberId}/messages");
        requisicao.Content = JsonContent.Create(payload);
        using var resposta = await http.SendAsync(requisicao);
        if (!resposta.IsSuccessStatusCode)
        {
            var texto = await resposta.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"[whatsapp] enviar: HTTP {(int)resposta.StatusCode} {(texto.Length > 300 ? texto[..300] : texto)}");
        }
    }

    private HttpRequestMessage Requisicao(HttpMethod metodo, string url)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opcoes.Token);
        return requisicao;
    }

    private sealed record InfoDaMidia(
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("mime_type")] string? TipoMime,
        [property: JsonPropertyName("file_size")] long TamanhoDoArquivo);
}
