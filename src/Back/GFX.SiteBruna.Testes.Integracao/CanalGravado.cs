using System.Collections.Concurrent;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Valores;
using SkiaSharp;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>WhatsApp de mentira: grava o que seria enviado e devolve JPEGs de verdade como mídia recebida.</summary>
public sealed class CanalGravado : ICanalWhatsapp
{
    public ConcurrentQueue<(string Telefone, string Texto, IReadOnlyList<BotaoDoWhatsapp>? Botoes)> Enviadas { get; } = new();

    public IEnumerable<(string Telefone, string Texto, IReadOnlyList<BotaoDoWhatsapp>? Botoes)> Para(string telefone) =>
        Enviadas.Where(e => e.Telefone == telefone);

    public Task EnviarTexto(string telefone, string texto)
    {
        Enviadas.Enqueue((telefone, texto, null));
        return Task.CompletedTask;
    }

    public Task EnviarBotoes(string telefone, string texto, IReadOnlyList<BotaoDoWhatsapp> botoes)
    {
        Enviadas.Enqueue((telefone, texto, botoes));
        return Task.CompletedTask;
    }

    public Task<byte[]> BaixarMidia(string midiaId)
    {
        using var bitmap = new SKBitmap(2400, 1600);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.SteelBlue);
        using var imagem = SKImage.FromBitmap(bitmap);
        using var jpeg = imagem.Encode(SKEncodedImageFormat.Jpeg, 80);
        return Task.FromResult(jpeg.ToArray());
    }
}
