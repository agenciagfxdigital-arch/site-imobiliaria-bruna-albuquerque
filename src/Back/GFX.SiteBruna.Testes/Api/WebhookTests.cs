using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GFX.SiteBruna.Api.Whatsapp;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Api;

public class WebhookTests
{
    private const string Segredo = "segredo-de-teste-123456";

    private static string Assinar(byte[] corpo) => $"sha256={Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Segredo), corpo))}";

    /// <summary>Payload no formato da WhatsApp Cloud API.</summary>
    private static JsonElement Payload(string mensagens, string phoneNumberId = "111") => JsonDocument.Parse($$"""
        {
          "object": "whatsapp_business_account",
          "entry": [{ "id": "WABA", "changes": [{ "field": "messages", "value": {
            "messaging_product": "whatsapp",
            "metadata": { "display_phone_number": "5549900000001", "phone_number_id": "{{phoneNumberId}}" },
            "contacts": [{ "profile": { "name": "Bruna" }, "wa_id": "554990000000" }],
            "messages": {{mensagens}}
          } }] }]
        }
        """).RootElement;

    [Fact]
    public void Aceita_a_assinatura_certa_e_recusa_corpo_alterado_cabecalho_ausente_ou_segredo_vazio()
    {
        var corpo = """{"ok":true}"""u8.ToArray();
        Assert.True(AssinaturaDoWebhook.Valida(corpo, Assinar(corpo), Segredo));
        Assert.False(AssinaturaDoWebhook.Valida("""{"ok":false}"""u8.ToArray(), Assinar(corpo), Segredo));
        Assert.False(AssinaturaDoWebhook.Valida(corpo, null, Segredo));
        Assert.False(AssinaturaDoWebhook.Valida(corpo, "sha256=abc", Segredo));
        Assert.False(AssinaturaDoWebhook.Valida(corpo, "sha256=zz", Segredo));
        Assert.False(AssinaturaDoWebhook.Valida(corpo, Assinar(corpo), ""));
    }

    [Fact]
    public void Traduz_texto_foto_foto_como_arquivo_botao_e_tipos_nao_suportados()
    {
        var lista = LeitorDoWebhook.Mensagens(Payload("""
            [
              { "from": "554990000000", "id": "wamid.1", "timestamp": "1792500000", "type": "text", "text": { "body": "ficha" } },
              { "from": "554990000000", "id": "wamid.2", "timestamp": "1792500001", "type": "image", "image": { "id": "M1", "mime_type": "image/jpeg", "caption": "BA107" } },
              { "from": "554990000000", "id": "wamid.3", "timestamp": "1792500002", "type": "document", "document": { "id": "M2", "mime_type": "image/png" } },
              { "from": "554990000000", "id": "wamid.4", "timestamp": "1792500003", "type": "interactive", "interactive": { "type": "button_reply", "button_reply": { "id": "ok:x", "title": "Publicar" } } },
              { "from": "554990000000", "id": "wamid.5", "timestamp": "1792500004", "type": "audio", "audio": { "id": "A1" } },
              { "from": "554990000000", "id": "wamid.6", "timestamp": "1792500005", "type": "document", "document": { "id": "P1", "mime_type": "application/pdf" } }
            ]
            """), "111");

        Assert.Equal(
            [
                new ConteudoMensagem.Texto("ficha"),
                new ConteudoMensagem.Imagem("M1", "image/jpeg", "BA107"),
                new ConteudoMensagem.Imagem("M2", "image/png", null),
                new ConteudoMensagem.Botao("ok:x", "Publicar"),
                new ConteudoMensagem.Outro("audio"),
                new ConteudoMensagem.Outro("documento"),
            ],
            lista.Select(m => m.Conteudo));
        Assert.Equal(("wamid.1", "554990000000", DateTimeOffset.FromUnixTimeSeconds(1792500000)),
            (lista[0].Wamid, lista[0].Telefone, lista[0].RecebidaEm));
    }

    [Fact]
    public void Ignora_status_de_entrega_mensagens_para_outro_numero_e_lixo()
    {
        var status = JsonDocument.Parse("""
            { "entry": [{ "changes": [{ "value": { "metadata": { "phone_number_id": "111" }, "statuses": [{ "id": "wamid.9", "status": "read" }] } }] }] }
            """).RootElement;
        Assert.Empty(LeitorDoWebhook.Mensagens(status, "111"));
        Assert.Empty(LeitorDoWebhook.Mensagens(Payload("""[{ "from": "5549", "id": "w", "type": "text", "text": { "body": "oi" } }]""", "999"), "111"));
        Assert.Empty(LeitorDoWebhook.Mensagens(JsonDocument.Parse("\"lixo\"").RootElement, "111"));
    }

    [Fact]
    public void Configuracao_incompleta_e_listada()
    {
        Assert.Equal(["WhatsApp:AppSecret", "WhatsApp:VerifyToken", "WhatsApp:Token", "WhatsApp:PhoneNumberId"],
            new OpcoesDoWebhook().Pendencias());
        Assert.Empty(new OpcoesDoWebhook { Modo = "simulado", AppSecret = Segredo, VerifyToken = Segredo }.Pendencias());
    }
}
