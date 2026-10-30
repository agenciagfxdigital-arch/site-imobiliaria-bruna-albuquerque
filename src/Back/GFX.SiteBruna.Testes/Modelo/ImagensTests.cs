using System.Text;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class ImagensTests
{
    private const string Id = "00000000-0000-4000-8000-000000000101";

    /// <summary>Monta um arquivo com a assinatura dada e completa com zeros.</summary>
    private static byte[] Arquivo(params object[] partes) =>
        [.. partes.SelectMany(p => p is string s ? Encoding.ASCII.GetBytes(s) : (byte[])p), .. new byte[16]];

    private static readonly byte[] Jpeg = Arquivo(new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 });
    private static readonly byte[] Png = Arquivo(new byte[] { 0x89 }, "PNG", new byte[] { 0x0D, 0x0A, 0x1A, 0x0A });
    private static readonly byte[] Webp = Arquivo("RIFF", new byte[4], "WEBP");
    private static readonly byte[] Avif = Arquivo(new byte[] { 0, 0, 0, 0x1C }, "ftypavif");
    private static readonly byte[] Heic = Arquivo(new byte[] { 0, 0, 0, 0x18 }, "ftypheic");

    [Fact]
    public void Reconhece_o_formato_pela_assinatura()
    {
        Assert.Equal(FormatoImagem.Jpeg, Imagens.DetectarFormato(Jpeg));
        Assert.Equal(FormatoImagem.Png, Imagens.DetectarFormato(Png));
        Assert.Equal(FormatoImagem.Webp, Imagens.DetectarFormato(Webp));
        Assert.Equal(FormatoImagem.Avif, Imagens.DetectarFormato(Avif));
        Assert.Equal(FormatoImagem.Heic, Imagens.DetectarFormato(Heic));
    }

    [Fact]
    public void Nao_reconhece_texto_nem_arquivo_curto()
    {
        Assert.Null(Imagens.DetectarFormato(Arquivo("<html><script>")));
        Assert.Null(Imagens.DetectarFormato(new byte[] { 0xFF, 0xD8 }));
    }

    [Fact]
    public void Aceita_jpeg_png_webp_e_avif()
    {
        foreach (var arquivo in new[] { Jpeg, Png, Webp, Avif }) Imagens.ValidarArquivo(arquivo);
    }

    [Fact]
    public void Recusa_heic_com_orientacao_para_o_usuario()
    {
        var erro = Assert.Throws<DomainException>(() => Imagens.ValidarArquivo(Heic));
        Assert.Contains("HEIC", erro.Message);
    }

    [Fact]
    public void Recusa_arquivo_vazio_grande_demais_ou_que_nao_e_imagem()
    {
        Assert.Throws<DomainException>(() => Imagens.ValidarArquivo([]));

        var grande = new byte[Imagens.TamanhoMaximoBytes + 1];
        grande[0] = 0xFF; grande[1] = 0xD8; grande[2] = 0xFF;
        Assert.Contains("15 MB", Assert.Throws<DomainException>(() => Imagens.ValidarArquivo(grande)).Message);

        Assert.Throws<DomainException>(() => Imagens.ValidarArquivo(Arquivo("%PDF-1.7 documento")));
    }

    [Fact]
    public void Aceita_pasta_de_imovel_e_pastas_do_site()
    {
        Assert.Equal(DestinoImagem.DoImovel(Guid.Parse(Id)), Imagens.InterpretarDestino($"imoveis/{Id}"));
        Assert.Equal(DestinoImagem.DoSite(PastaDoSite.Entregas), Imagens.InterpretarDestino("entregas"));
    }

    [Theory]
    [InlineData("../segredos")]
    [InlineData("imoveis/abc")]
    [InlineData("imoveis/" + Id + "/../x")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("admin")]
    [InlineData("Site")]
    public void Recusa_caminhos_fora_do_previsto(string? pasta)
    {
        Assert.Throws<DomainException>(() => Imagens.InterpretarDestino(pasta));
    }

    [Fact]
    public void Monta_o_caminho_sempre_em_webp()
    {
        Assert.Equal($"imoveis/{Id}/abc.webp", Imagens.CaminhoDoArquivo(DestinoImagem.DoImovel(Guid.Parse(Id)), "abc"));
        Assert.Equal("site/abc.webp", Imagens.CaminhoDoArquivo(DestinoImagem.DoSite(PastaDoSite.Site), "abc"));
    }

    [Fact]
    public void Avisa_so_quando_a_foto_e_pequena()
    {
        Assert.Null(Imagens.AvisoDeTamanho(1920, 1080));
        Assert.Contains("pequena", Imagens.AvisoDeTamanho(800, 500));
    }
}
