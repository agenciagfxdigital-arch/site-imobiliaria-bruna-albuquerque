using GFX.SiteBruna.Aplicacao.DTOs.Imagens;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>
/// Converte a imagem para o padrão do site com SkiaSharp (MIT): aplica a orientação do EXIF, limita o lado maior e grava
/// em WebP a partir dos pixels, sem nenhum metadado (o EXIF de celular traz o GPS, que revelaria o endereço do imóvel).
/// O SkiaSharp não lê AVIF nem HEIC: essas fotos são recusadas com mensagem para a Bruna.
/// </summary>
internal sealed class ProcessadorSkia(ILogger<ProcessadorSkia> logger) : IProcessadorDeImagens
{
    /// <summary>Recusa "bombas" de descompressão (pequenas em bytes e gigantes em pixels).</summary>
    private const long LimiteDePixels = 100_000_000;

    public Task<ImagemProcessada> Processar(byte[] conteudo)
    {
        try
        {
            using var dados = SKData.CreateCopy(conteudo);
            using var codec = SKCodec.Create(dados) ?? throw new InvalidOperationException("formato não suportado");
            if ((long)codec.Info.Width * codec.Info.Height > LimiteDePixels) throw new InvalidOperationException("imagem grande demais");

            using var original = SKBitmap.Decode(codec) ?? throw new InvalidOperationException("não decodificou");
            using var orientada = AplicarOrientacao(original, codec.EncodedOrigin);
            using var reduzida = Reduzir(orientada);
            using var imagem = SKImage.FromBitmap(reduzida);
            using var webp = imagem.Encode(SKEncodedImageFormat.Webp, Imagens.QualidadeWebp)
                ?? throw new InvalidOperationException("não codificou em WebP");

            return Task.FromResult(new ImagemProcessada(webp.ToArray(), "image/webp", reduzida.Width, reduzida.Height));
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "[imagens] não consegui processar a imagem");
            throw DomainException.Validacao("Não consegui ler essa imagem. Tente outra foto (JPEG, PNG ou WebP).");
        }
    }

    private static SKBitmap Reduzir(SKBitmap bitmap)
    {
        var maior = Math.Max(bitmap.Width, bitmap.Height);
        if (maior <= Imagens.LadoMaximoPx) return bitmap.Copy();
        var escala = (double)Imagens.LadoMaximoPx / maior;
        var info = new SKImageInfo((int)Math.Round(bitmap.Width * escala), (int)Math.Round(bitmap.Height * escala));
        return bitmap.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
    }

    /// <summary>Gira/espelha os pixels conforme o EXIF, para a foto não aparecer deitada depois que o EXIF sai.</summary>
    private static SKBitmap AplicarOrientacao(SKBitmap bitmap, SKEncodedOrigin origem)
    {
        if (origem == SKEncodedOrigin.TopLeft) return bitmap.Copy();
        var troca = origem is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var resultado = new SKBitmap(troca ? bitmap.Height : bitmap.Width, troca ? bitmap.Width : bitmap.Height);
        using var canvas = new SKCanvas(resultado);
        float w = bitmap.Width, h = bitmap.Height;
        switch (origem)
        {
            case SKEncodedOrigin.TopRight: canvas.Scale(-1, 1); canvas.Translate(-w, 0); break;
            case SKEncodedOrigin.BottomRight: canvas.RotateDegrees(180); canvas.Translate(-w, -h); break;
            case SKEncodedOrigin.BottomLeft: canvas.Scale(1, -1); canvas.Translate(0, -h); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(h, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(h, w); canvas.RotateDegrees(90); canvas.Scale(-1, 1); canvas.Translate(-w, 0); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, w); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(bitmap, 0, 0);
        return resultado;
    }
}
