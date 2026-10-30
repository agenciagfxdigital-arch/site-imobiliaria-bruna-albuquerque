using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class PlanejamentoDeFotosTests
{
    private static readonly Guid ImovelId = Guid.CreateVersion7();
    private static readonly FotoImovel A = new(ImovelId, "a", "imoveis/1/a.webp", 0, true);
    private static readonly FotoImovel B = new(ImovelId, "b", "imoveis/1/b.webp", 1, false);
    private static readonly FotoImovel[] Atuais = [A, B];

    private static FotoDesejada Manter(FotoImovel f, bool capa = false) => new(f.Id, f.Url, f.CaminhoNoStorage, capa);

    [Fact]
    public void Segue_a_ordem_da_lista_e_mantem_a_capa_escolhida()
    {
        var plano = PlanejamentoDeFotos.Planejar(Atuais, [Manter(B, capa: true), Manter(A)]);

        Assert.Equal([(B.Id, 0, true), (A.Id, 1, false)], plano.Fotos.Select(f => (f.Id!.Value, f.Ordem, f.Capa)));
        Assert.Empty(plano.Removidas);
    }

    [Fact]
    public void Sem_capa_marcada_a_primeira_vira_capa()
    {
        var plano = PlanejamentoDeFotos.Planejar(Atuais, [Manter(A), Manter(B)]);
        Assert.Equal([true, false], plano.Fotos.Select(f => f.Capa));
    }

    [Fact]
    public void Com_varias_marcadas_so_a_primeira_continua_capa()
    {
        var plano = PlanejamentoDeFotos.Planejar(Atuais, [Manter(A, capa: true), Manter(B, capa: true)]);
        Assert.Equal([true, false], plano.Fotos.Select(f => f.Capa));
    }

    [Fact]
    public void Marca_como_removidas_as_que_sairam_e_inclui_as_novas_sem_id()
    {
        var plano = PlanejamentoDeFotos.Planejar(Atuais, [Manter(B), new FotoDesejada(null, "nova", "imoveis/1/nova.webp", false)]);

        Assert.Equal([A], plano.Removidas);
        Assert.Equal(new FotoPlanejada(null, "nova", "imoveis/1/nova.webp", 1, false), plano.Fotos[1]);
    }

    [Fact]
    public void Recusa_foto_de_outro_imovel()
    {
        var deOutro = new FotoDesejada(Guid.CreateVersion7(), "c", null, true);
        Assert.Throws<DomainException>(() => PlanejamentoDeFotos.Planejar(Atuais, [deOutro]));
    }

    [Fact]
    public void Lista_vazia_remove_todas()
    {
        Assert.Equal(2, PlanejamentoDeFotos.Planejar(Atuais, []).Removidas.Count);
    }

    [Fact]
    public void Recusa_mais_fotos_que_o_limite()
    {
        var muitas = Enumerable.Range(0, PlanejamentoDeFotos.MaximoPorImovel + 1)
            .Select(_ => new FotoDesejada(null, "x", null, false)).ToArray();
        Assert.Throws<DomainException>(() => PlanejamentoDeFotos.Planejar(Atuais, muitas));
    }

    [Fact]
    public void Apaga_so_os_arquivos_que_nenhuma_foto_usa_mais_sem_repetir()
    {
        var apagar = PlanejamentoDeFotos.ArquivosParaApagar(["a.webp", "b.webp", "a.webp", null], new HashSet<string> { "b.webp" });
        Assert.Equal(["a.webp"], apagar);
    }
}
