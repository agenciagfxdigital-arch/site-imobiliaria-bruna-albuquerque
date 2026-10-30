using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>WhatsApp:Modo = "simulado": nada vai para a Meta. As respostas aparecem no log e as fotos são geradas.</summary>
internal sealed class CanalSimulado(ILogger<CanalSimulado> logger) : ICanalWhatsapp
{
    public Task EnviarTexto(string telefone, string texto)
    {
        logger.LogInformation("[whatsapp simulado] → {Telefone}\n{Texto}", telefone, texto);
        return Task.CompletedTask;
    }

    public Task EnviarBotoes(string telefone, string texto, IReadOnlyList<BotaoDoWhatsapp> botoes)
    {
        logger.LogInformation("[whatsapp simulado] → {Telefone}\n{Texto}\n  [botões] {Botoes}",
            telefone, texto, string.Join(" | ", botoes.Select(b => $"{b.Titulo} = {b.Id}")));
        return Task.CompletedTask;
    }

    /// <summary>Foto de teste (1200×900) com uma cor que depende do id da mídia.</summary>
    public Task<byte[]> BaixarMidia(string midiaId)
    {
        var h = (uint)midiaId.Aggregate(17, (acc, c) => unchecked(acc * 31 + c));
        using var bitmap = new SKBitmap(1200, 900);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(new SKColor((byte)h, (byte)(h >> 8), (byte)(h >> 16)));
        using var imagem = SKImage.FromBitmap(bitmap);
        using var jpeg = imagem.Encode(SKEncodedImageFormat.Jpeg, 85);
        return Task.FromResult(jpeg.ToArray());
    }
}
