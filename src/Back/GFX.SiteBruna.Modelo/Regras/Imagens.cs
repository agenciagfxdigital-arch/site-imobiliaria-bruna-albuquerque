using System.Text;
using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>
/// Regras para receber imagens (fotos de imóveis, da Bruna, de depoimentos e entregas de chave).
/// O processamento em si (redimensionar, converter, tirar EXIF) fica no Dados.
/// </summary>
public static partial class Imagens
{
    /// <summary>Tamanho máximo do arquivo recebido (antes do processamento).</summary>
    public const int TamanhoMaximoBytes = 15 * 1024 * 1024;
    /// <summary>Lado maior da imagem gravada.</summary>
    public const int LadoMaximoPx = 1920;
    /// <summary>Abaixo disso a foto é aceita, mas quem enviou recebe um aviso.</summary>
    public const int LadoMinimoRecomendadoPx = 600;
    public const int QualidadeWebp = 80;

    private static readonly string[] MarcasHeic = ["heic", "heix", "hevc", "hevx", "mif1", "msf1"];

    /// <summary>
    /// Identifica o formato pelos primeiros bytes (assinatura), e não pelo nome ou tipo informado,
    /// que podem estar errados ou ser falsificados.
    /// </summary>
    public static FormatoImagem? DetectarFormato(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12) return null;
        string Ascii(ReadOnlySpan<byte> b, int inicio, int fim) => Encoding.ASCII.GetString(b[inicio..fim]);

        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return FormatoImagem.Jpeg;
        if (bytes[0] == 0x89 && Ascii(bytes, 1, 4) == "PNG") return FormatoImagem.Png;
        if (Ascii(bytes, 0, 4) == "RIFF" && Ascii(bytes, 8, 12) == "WEBP") return FormatoImagem.Webp;
        if (Ascii(bytes, 4, 8) == "ftyp")
        {
            var marca = Ascii(bytes, 8, 12);
            if (marca is "avif" or "avis") return FormatoImagem.Avif;
            if (MarcasHeic.Contains(marca)) return FormatoImagem.Heic;
        }
        return null;
    }

    /// <summary>Valida o arquivo recebido e devolve o formato (nunca <see cref="FormatoImagem.Heic"/>).</summary>
    public static FormatoImagem ValidarArquivo(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0) throw DomainException.Validacao("O arquivo está vazio.");
        if (bytes.Length > TamanhoMaximoBytes) throw DomainException.Validacao("A foto é grande demais (máximo de 15 MB).");
        return DetectarFormato(bytes) switch
        {
            FormatoImagem.Heic => throw DomainException.Validacao(
                "Fotos HEIC do iPhone não são aceitas. Envie em JPEG (ou pelo WhatsApp, que já converte)."),
            { } formato => formato,
            null => throw DomainException.Validacao("Esse arquivo não é uma imagem aceita. Envie JPEG, PNG ou WebP."),
        };
    }

    /// <summary>Aviso (não bloqueia) para fotos pequenas, que ficam borradas no site.</summary>
    public static string? AvisoDeTamanho(int largura, int altura) =>
        Math.Min(largura, altura) < LadoMinimoRecomendadoPx
            ? $"A foto é pequena ({largura}×{altura} px) e pode ficar borrada no site."
            : null;

    /// <summary>
    /// Converte o texto de pasta usado pelo painel num destino válido: <c>imoveis/&lt;id&gt;</c> ou uma pasta do site.
    /// Qualquer outra coisa é recusada (impede gravar fora das pastas previstas, como "../").
    /// </summary>
    public static DestinoImagem InterpretarDestino(string? pasta)
    {
        if (pasta is not null)
        {
            var imovel = PastaDeImovel().Match(pasta);
            if (imovel.Success && Guid.TryParseExact(imovel.Groups[1].Value, "D", out var id)) return DestinoImagem.DoImovel(id);
            foreach (var p in Enum.GetValues<PastaDoSite>())
                if (pasta == p.ToString().ToLowerInvariant()) return DestinoImagem.DoSite(p);
        }
        throw DomainException.Validacao("Destino da imagem inválido.");
    }

    /// <summary>Caminho do arquivo no storage. As imagens gravadas são sempre WebP.</summary>
    public static string CaminhoDoArquivo(DestinoImagem destino, string id) => $"{destino.NomeDaPasta}/{id}.webp";

    [GeneratedRegex("^imoveis/([0-9a-fA-F-]{36})$")]
    private static partial Regex PastaDeImovel();
}
