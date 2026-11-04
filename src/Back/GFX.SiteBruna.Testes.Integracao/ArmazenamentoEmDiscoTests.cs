using GFX.SiteBruna.Dados.Servicos;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Testes.Integracao;

public sealed class ArmazenamentoEmDiscoTests : IDisposable
{
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"site-bruna-disco-{Guid.NewGuid():N}");

    private ArmazenamentoEmDisco Armazenamento(string pasta = "fotos") => new(
        Options.Create(new OpcoesDasFotos { Pasta = pasta }),
        new HostingEnvironment { ContentRootPath = _raiz },
        NullLogger<ArmazenamentoEmDisco>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_raiz)) Directory.Delete(_raiz, recursive: true);
    }

    [Fact]
    public async Task Grava_na_pasta_e_devolve_url_relativa()
    {
        var url = await Armazenamento().Enviar("imoveis/1/foto.webp", [1, 2, 3], "image/webp");

        Assert.Equal("/media/imoveis/1/foto.webp", url);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Path.Combine(_raiz, "fotos", "imoveis", "1", "foto.webp")));
        Assert.Single(Directory.GetFiles(Path.Combine(_raiz, "fotos", "imoveis", "1")));
    }

    [Fact]
    public async Task Nao_sobrescreve_arquivo_existente()
    {
        var armazenamento = Armazenamento();
        await armazenamento.Enviar("site/a.webp", [1], "image/webp");

        await Assert.ThrowsAsync<IOException>(() => armazenamento.Enviar("site/a.webp", [2], "image/webp"));

        Assert.Equal([1], await File.ReadAllBytesAsync(Path.Combine(_raiz, "fotos", "site", "a.webp")));
        Assert.Single(Directory.GetFiles(Path.Combine(_raiz, "fotos", "site")));
    }

    [Fact]
    public async Task Remove_e_ignora_o_que_nao_existe()
    {
        var armazenamento = Armazenamento();
        await armazenamento.Enviar("site/a.webp", [1], "image/webp");

        await armazenamento.Remover(["site/a.webp", "site/nao-existe.webp", "outra/nao-existe.webp"]);

        Assert.False(File.Exists(Path.Combine(_raiz, "fotos", "site", "a.webp")));
    }

    [Theory]
    [InlineData("../fora.webp")]
    [InlineData("site/../../fora.webp")]
    public async Task Recusa_caminho_fora_da_pasta(string caminho)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Armazenamento().Enviar(caminho, [1], "image/webp"));

        Assert.False(File.Exists(Path.Combine(_raiz, "fora.webp")));
    }

    [Fact]
    public async Task Remover_nao_apaga_nada_fora_da_pasta()
    {
        Directory.CreateDirectory(_raiz);
        var fora = Path.Combine(_raiz, "fora.webp");
        await File.WriteAllBytesAsync(fora, [1]);

        await Armazenamento().Remover(["../fora.webp"]);

        Assert.True(File.Exists(fora));
    }

    [Fact]
    public async Task Sem_pasta_configurada_avisa_o_que_falta()
    {
        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() => Armazenamento("").Enviar("a.webp", [1], "image/webp"));

        Assert.Contains("Fotos:Pasta", erro.Message);
    }
}
