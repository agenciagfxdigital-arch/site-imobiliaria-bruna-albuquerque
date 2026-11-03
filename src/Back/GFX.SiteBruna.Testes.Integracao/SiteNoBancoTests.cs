using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Aplicacao.DTOs.Site;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.Extensions.DependencyInjection;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>
/// Consultas do site público no Postgres de verdade: a regra de visibilidade e as ordens viram SQL. Cada teste usa um
/// bairro próprio para não enxergar os imóveis dos outros (o banco é compartilhado pela classe).
/// </summary>
public class SiteNoBancoTests(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    private async Task<T> Com<TServico, T>(Func<TServico, Task<ApiResponse<T>>> chamada) where TServico : notnull
    {
        await using var escopo = banco.Escopo();
        var resposta = await chamada(escopo.ServiceProvider.GetRequiredService<TServico>());
        Assert.True(resposta.Sucesso, string.Join(" ", resposta.Mensagens));
        return resposta.Dados!;
    }

    private Task<BairroResponse> NovoBairro() =>
        Com<BairroService, BairroResponse>(s => s.Salvar(new(null, $"Bairro {Guid.NewGuid():N}"[..20])));

    private async Task<ImovelSalvoResponse> Novo(DadosDoImovel dados, BairroResponse bairro, int fotos = 0)
    {
        var salvo = await Com<ImovelService, ImovelSalvoResponse>(s => s.Salvar(new(null, dados with { BairroId = bairro.Id })));
        if (fotos > 0)
            await Com<ImovelService, IReadOnlyList<FotoResponse>>(s => s.SalvarFotos(salvo.Id,
            [
                .. Enumerable.Range(0, fotos).Select(i => new FotoDesejada(null, $"https://storage.teste/{salvo.Id}/{i}.webp", null, i == 1)),
            ]));
        return salvo;
    }

    private static DadosDoImovel Venda(string titulo, decimal? preco = 300_000, StatusImovel status = StatusImovel.Ativo) => new()
    {
        Finalidade = Finalidade.Venda, Tipo = TipoImovel.Apartamento, Titulo = titulo, Preco = preco, Quartos = 2,
        Vagas = 1, Status = status,
    };

    private async Task<IReadOnlyList<string>> Codigos(FiltroDoSite filtro) =>
        [.. (await Com<ImovelDoSiteService, ListaDeImoveisResponse>(s => s.Pesquisar(filtro))).Itens.Select(i => i.Codigo)];

    [Fact]
    public async Task Lista_filtra_pelo_bairro_ordena_pelo_preco_e_deixa_os_vendidos_no_fim()
    {
        var bairro = await NovoBairro();
        var caro = await Novo(Venda("Apartamento caro", 500_000), bairro);
        var vendido = await Novo(Venda("Apartamento vendido", 100_000, StatusImovel.Vendido), bairro);
        var consulte = await Novo(Venda("Apartamento sem preço", null), bairro);
        var barato = await Novo(Venda("Apartamento barato", 200_000), bairro);
        await Novo(Venda("Rascunho no bairro", 150_000, StatusImovel.Rascunho), bairro);

        Assert.Equal([barato.Codigo, caro.Codigo, consulte.Codigo, vendido.Codigo],
            await Codigos(new FiltroDoSite { Bairro = bairro.Slug, Ordem = OrdemDoSite.MenorPreco }));
        Assert.Equal([caro.Codigo, barato.Codigo, consulte.Codigo, vendido.Codigo],
            await Codigos(new FiltroDoSite { Bairro = bairro.Slug, Ordem = OrdemDoSite.MaiorPreco }));
        Assert.Equal([barato.Codigo, consulte.Codigo, caro.Codigo, vendido.Codigo],
            await Codigos(new FiltroDoSite { Bairro = bairro.Slug }));
        Assert.Equal([barato.Codigo],
            await Codigos(new FiltroDoSite { Bairro = bairro.Slug, PrecoMinimo = 150_000, PrecoMaximo = 300_000 }));
    }

    [Fact]
    public async Task Selo_de_vendido_vence_pela_regra_do_Modelo_traduzida_para_SQL()
    {
        var bairro = await NovoBairro();
        var vendido = await Novo(Venda("Apartamento vendido", status: StatusImovel.Vendido), bairro);
        var filtro = new FiltroDoSite { Bairro = bairro.Slug };

        await using var escopo = banco.Escopo();
        var repositorio = escopo.ServiceProvider.GetRequiredService<IImovelDoSiteRepository>();
        var agora = DateTimeOffset.UtcNow;

        Assert.Equal(1, (await repositorio.Pesquisar(filtro, bairro.Id, agora.AddDays(29), 30)).Total);
        Assert.Equal(0, (await repositorio.Pesquisar(filtro, bairro.Id, agora.AddDays(31), 30)).Total);
        Assert.NotNull(await repositorio.GetVisivelByCodigo(vendido.Codigo, agora, 30));
        Assert.Null(await repositorio.GetVisivelByCodigo(vendido.Codigo, agora.AddDays(31), 30));
    }

    [Fact]
    public async Task Filtros_de_aluguel_e_paginacao_viram_SQL()
    {
        var bairro = await NovoBairro();
        var aluguel = Venda("Apartamento para alugar") with { Finalidade = Finalidade.Aluguel, Aluguel = 1_500, Mobiliado = true, AceitaPet = true };
        var mobiliado = await Novo(aluguel, bairro);
        await Novo(aluguel with { Titulo = "Apartamento sem pet", AceitaPet = false }, bairro);
        for (var i = 0; i < 12; i++) await Novo(aluguel with { Titulo = $"Apartamento número {i}", Mobiliado = false }, bairro);

        Assert.Equal([mobiliado.Codigo],
            await Codigos(new FiltroDoSite { Finalidade = Finalidade.Aluguel, Bairro = bairro.Slug, Mobiliado = true, AceitaPet = true }));

        var segunda = await Com<ImovelDoSiteService, ListaDeImoveisResponse>(s =>
            s.Pesquisar(new FiltroDoSite { Finalidade = Finalidade.Aluguel, Bairro = bairro.Slug, Pagina = 2 }));
        Assert.Equal(14, segunda.Total);
        Assert.Equal(2, segunda.Itens.Count);
    }

    [Fact]
    public async Task Pagina_do_imovel_traz_fotos_capa_semelhantes_e_o_sitemap_so_ativos()
    {
        var bairro = await NovoBairro();
        var imovel = await Novo(Venda("Apartamento com sacada"), bairro, fotos: 3);
        var semelhante = await Novo(Venda("Casa no mesmo bairro") with { Tipo = TipoImovel.Casa }, bairro);
        var rascunho = await Novo(Venda("Rascunho no bairro", status: StatusImovel.Rascunho), bairro);

        var pagina = await Com<ImovelDoSiteService, PaginaDoImovelResponse>(s => s.Obter($"{imovel.Codigo.ToLowerInvariant()}-qualquer"));

        Assert.Equal($"https://storage.teste/{imovel.Id}/1.webp", pagina.Imovel.Fotos[0].Url);
        Assert.Equal(3, pagina.Imovel.Fotos.Count);
        Assert.Equal(bairro.Nome, pagina.Imovel.Bairro!.Nome);
        Assert.Contains(semelhante.Codigo, pagina.Semelhantes.Select(s => s.Codigo));

        var sitemap = await Com<ImovelDoSiteService, IReadOnlyList<ImovelNoSitemapResponse>>(s => s.Sitemap());
        Assert.Contains(sitemap, i => i.Caminho == pagina.Imovel.Caminho);
        Assert.DoesNotContain(sitemap, i => i.Caminho.StartsWith($"/imovel/{rascunho.Codigo.ToLowerInvariant()}-"));
    }

    [Fact]
    public async Task Destaques_da_home_tem_a_capa_do_imovel()
    {
        var bairro = await NovoBairro();
        var destaque = await Novo(Venda("Apartamento em destaque") with { Destaque = true }, bairro, fotos: 2);

        var destaques = await Com<ImovelDoSiteService, IReadOnlyList<CartaoDeImovelResponse>>(s => s.Destaques());

        var cartao = Assert.Single(destaques, d => d.Codigo == destaque.Codigo);
        Assert.Equal($"https://storage.teste/{destaque.Id}/1.webp", cartao.CapaUrl);
        Assert.Equal(bairro.Nome, cartao.Bairro);
    }
}
