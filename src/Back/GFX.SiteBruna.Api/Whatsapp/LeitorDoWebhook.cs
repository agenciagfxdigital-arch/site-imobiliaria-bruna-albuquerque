using System.Text.Json;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Api.Whatsapp;

/// <summary>
/// Traduz o payload da WhatsApp Cloud API para <see cref="MensagemRecebida"/>. Ignora status de entrega/leitura e
/// mensagens destinadas a outro número.
/// </summary>
public static class LeitorDoWebhook
{
    public static IReadOnlyList<MensagemRecebida> Mensagens(JsonElement payload, string? phoneNumberId)
    {
        List<MensagemRecebida> lista = [];
        foreach (var entrada in Itens(payload, "entry"))
        foreach (var mudanca in Itens(entrada, "changes"))
        {
            if (!mudanca.TryGetProperty("value", out var valor) || valor.ValueKind != JsonValueKind.Object) continue;
            if (!string.IsNullOrEmpty(phoneNumberId) && Texto(Objeto(valor, "metadata"), "phone_number_id") != phoneNumberId) continue;
            foreach (var m in Itens(valor, "messages"))
            {
                var wamid = Texto(m, "id");
                var telefone = Texto(m, "from");
                if (wamid is null || telefone is null) continue;
                var segundos = long.TryParse(Texto(m, "timestamp"), out var s) && s > 0 ? s : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                lista.Add(new MensagemRecebida(wamid, telefone, DateTimeOffset.FromUnixTimeSeconds(segundos), Conteudo(m)));
            }
        }
        return lista;
    }

    private static ConteudoMensagem Conteudo(JsonElement m)
    {
        switch (Texto(m, "type"))
        {
            case "text":
                return new ConteudoMensagem.Texto(Texto(Objeto(m, "text"), "body") ?? "");
            case "image":
                var imagem = Objeto(m, "image");
                return new ConteudoMensagem.Imagem(Texto(imagem, "id") ?? "", Texto(imagem, "mime_type"), Texto(imagem, "caption"));
            case "document":
                // Foto enviada "como arquivo" (qualidade original) chega como documento.
                var documento = Objeto(m, "document");
                var mime = Texto(documento, "mime_type");
                return mime?.StartsWith("image/") == true
                    ? new ConteudoMensagem.Imagem(Texto(documento, "id") ?? "", mime, Texto(documento, "caption"))
                    : new ConteudoMensagem.Outro("documento");
            case "interactive":
                var resposta = Objeto(Objeto(m, "interactive"), "button_reply");
                return new ConteudoMensagem.Botao(Texto(resposta, "id") ?? "", Texto(resposta, "title"));
            case "button":
                var botao = Objeto(m, "button");
                return new ConteudoMensagem.Botao(Texto(botao, "payload") ?? "", Texto(botao, "text"));
            case { } tipo:
                return new ConteudoMensagem.Outro(tipo);
            default:
                return new ConteudoMensagem.Outro("desconhecido");
        }
    }

    private static IEnumerable<JsonElement> Itens(JsonElement e, string nome) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var lista) && lista.ValueKind == JsonValueKind.Array
            ? lista.EnumerateArray()
            : [];

    private static JsonElement Objeto(JsonElement e, string nome) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var o) && o.ValueKind == JsonValueKind.Object ? o : default;

    private static string? Texto(JsonElement e, string nome) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(nome, out var t) && t.ValueKind == JsonValueKind.String
        && t.GetString() is { Length: > 0 } s
            ? s
            : null;
}
