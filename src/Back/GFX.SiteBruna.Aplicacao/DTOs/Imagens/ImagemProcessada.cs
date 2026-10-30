namespace GFX.SiteBruna.Aplicacao.DTOs.Imagens;

/// <param name="Tipo">Sempre "image/webp".</param>
public sealed record ImagemProcessada(byte[] Conteudo, string Tipo, int Largura, int Altura);
