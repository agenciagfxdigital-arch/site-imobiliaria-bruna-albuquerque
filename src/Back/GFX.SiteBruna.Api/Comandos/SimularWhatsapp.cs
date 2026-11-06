using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GFX.SiteBruna.Aplicacao.DTOs.Whatsapp;

namespace GFX.SiteBruna.Api.Comandos;

/// <summary>
/// Simula mensagens do WhatsApp chegando no webhook da Api que está rodando, no formato da Meta e assinadas, e mostra o que
/// o sistema responderia. Nada vai para a Meta. Com a Api aberta, rode com <c>--no-build</c> (docs/CONFIGURACAO.md):
/// <c>dotnet run --no-build --project src/Back/GFX.SiteBruna.Api -- simular-whatsapp 5549999999999 texto ficha</c>.
/// </summary>
public static class SimularWhatsapp
{
    public const string Nome = "simular-whatsapp";

    private const string Uso = "Uso: simular-whatsapp <telefone> texto <mensagem> | arquivo <caminho> | fotos <quantidade> [legenda] | botao <id>";
    private static readonly TimeSpan EsperaDoTexto = TimeSpan.FromSeconds(4);
    /// <summary>O lote de fotos fecha depois de 15 s sem foto nova.</summary>
    private static readonly TimeSpan EsperaDasFotos = TimeSpan.FromSeconds(20);

    public static async Task<int> Executar(IConfiguration configuracao, string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var segredo = configuracao["WhatsApp:AppSecret"];
        if (args.Length < 4 || string.IsNullOrEmpty(segredo))
        {
            Console.Error.WriteLine(Uso);
            Console.Error.WriteLine("Precisa também de WhatsApp:Habilitado=true, WhatsApp:AppSecret e WhatsApp:VerifyToken.");
            return 1;
        }
        var (telefone, comando, valores) = (args[1], args[2], args[3..]);
        var mensagens = Mensagens(telefone, comando, valores);
        if (mensagens is null)
        {
            Console.Error.WriteLine(Uso);
            return 1;
        }

        using var http = new HttpClient { BaseAddress = new Uri(configuracao["WhatsApp:UrlDaSimulacao"] ?? "http://localhost:5130/") };
        // Fotos chegam em webhooks separados, como na Meta
        if (comando == "fotos") await Task.WhenAll(mensagens.Select(m => Enviar(http, segredo, [m])));
        else await Enviar(http, segredo, mensagens);

        var espera = comando == "fotos" ? EsperaDasFotos : EsperaDoTexto;
        Console.WriteLine($"Enviado. Aguardando {espera.TotalSeconds:0} s pela resposta...\n");
        await Task.Delay(espera);

        var resposta = await http.GetAsync("api/whatsapp/webhook/saida");
        if (!resposta.IsSuccessStatusCode)
        {
            Console.Error.WriteLine("Não consegui ler a saída: confira WhatsApp:Modo=simulado e WhatsApp:Habilitado=true.");
            return 1;
        }
        var saida = await resposta.Content.ReadFromJsonAsync<List<MensagemSimulada>>() ?? [];
        if (saida.Count == 0) Console.WriteLine("(nenhuma resposta: o telefone está cadastrado como operador?)");
        foreach (var mensagem in saida)
        {
            Console.WriteLine($"→ {mensagem.Telefone}\n{mensagem.Texto}");
            foreach (var botao in mensagem.Botoes) Console.WriteLine($"   [{botao.Titulo}]  simular-whatsapp {telefone} botao {botao.Id}");
            Console.WriteLine();
        }
        return 0;
    }

    private static List<Dictionary<string, object>>? Mensagens(string telefone, string comando, string[] valores)
    {
        Dictionary<string, object> Mensagem(string tipo, object conteudo) => new()
        {
            ["from"] = telefone,
            ["id"] = $"wamid.SIM{Guid.NewGuid():N}",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["type"] = tipo,
            [tipo] = conteudo,
        };

        switch (comando)
        {
            case "texto":
                return [Mensagem("text", new { body = string.Join(' ', valores) })];
            case "arquivo":
                return [Mensagem("text", new { body = File.ReadAllText(valores[0]) })];
            case "fotos":
                var quantidade = int.TryParse(valores[0], out var n) ? Math.Max(1, n) : 1;
                var legenda = valores.ElementAtOrDefault(1) ?? "";
                return
                [
                    .. Enumerable.Range(0, quantidade).Select(i => Mensagem("image",
                        new { id = $"simulada-{Guid.NewGuid():N}", mime_type = "image/jpeg", caption = i == 0 ? legenda : "" })),
                ];
            case "botao":
                return [Mensagem("interactive", new { type = "button_reply", button_reply = new { id = valores[0], title = "Botão" } })];
            default:
                return null;
        }
    }

    private static async Task Enviar(HttpClient http, string segredo, IReadOnlyList<Dictionary<string, object>> mensagens)
    {
        var corpo = JsonSerializer.SerializeToUtf8Bytes(new
        {
            @object = "whatsapp_business_account",
            entry = new[]
            {
                new { id = "SIMULADO", changes = new[] { new { field = "messages", value = new { messaging_product = "whatsapp", metadata = new { }, messages = mensagens } } } },
            },
        });
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "api/whatsapp/webhook") { Content = new ByteArrayContent(corpo) };
        requisicao.Content.Headers.ContentType = new("application/json");
        requisicao.Headers.Add("X-Hub-Signature-256", $"sha256={Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), corpo))}");
        var resposta = await http.SendAsync(requisicao);
        if (!resposta.IsSuccessStatusCode) throw new InvalidOperationException($"O webhook respondeu HTTP {(int)resposta.StatusCode}.");
    }
}
