using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Site;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Aplicacao;

public class ImovelDoSiteServiceTests
{
    private readonly Mundo _m = new();

    private static T Ok<T>(ApiResponse<T> r)
    {
        Assert.True(r.Sucesso, string.Join(" ", r.Mensagens));
        return r.Dados!;
    }

    private static DadosDoImovel Aluguel(decimal? valor, string titulo = "Apartamento para alugar no Centro") =>
        Mundo.Venda(titulo) with { Finalidade = Finalidade.Aluguel, Aluguel = valor };

    private async Task<IReadOnlyList<string>> Codigos(FiltroDoSite filtro) =>
        [.. Ok(await _m.ImovelDoSite.Pesquisar(filtro)).Itens.Select(i => i.Codigo)];

    // --- Lista com filtros ---------------------------------------------------------------------------

    [Fact]
    public async Task Lista_so_o_que_esta_no_site_com_os_vendidos_por_ultimo_e_o_selo_vencendo()
    {
        var antigo = _m.CriarImovel(Mundo.Venda("Apartamento antigo"));
        var vendido = _m.CriarImovel(Mundo.Venda("Apartamento vendido", StatusImovel.Vendido));
        _m.CriarImovel(Mundo.Venda("Rascunho", StatusImovel.Rascunho));
        _m.CriarImovel(Mundo.Venda("Pausado", StatusImovel.Pausado));
        var novo = _m.CriarImovel(Mundo.Venda("Apartamento novo"));

        Assert.Equal([novo.Codigo, antigo.Codigo, vendido.Codigo], await Codigos(new FiltroDoSite()));

        _m.Avancar(TimeSpan.FromDays(31));
        Assert.Equal([novo.Codigo, antigo.Codigo], await Codigos(new FiltroDoSite()));
    }

    [Fact]
    public async Task Selo_usa_os_dias_das_configuracoes()
    {
        var vendido = _m.CriarImovel(Mundo.Venda("Apartamento vendido", StatusImovel.Vendido));
        _m.DefinirDiasDoSelo(5);
        _m.Avancar(TimeSpan.FromDays(6));

        Assert.Empty(await Codigos(new FiltroDoSite()));
        Assert.Equal(TipoFalha.NaoEncontrado, (await _m.ImovelDoSite.Obter(vendido.Codigo)).Falha);
    }

    [Fact]
    public async Task Faixa_de_preco_usa_o_valor_do_aluguel_no_aluguel_e_o_preco_na_venda()
    {
        _m.CriarImovel(Aluguel(1_000));
        var meio = _m.CriarImovel(Aluguel(1_500));
        _m.CriarImovel(Aluguel(2_600));
        var venda = _m.CriarImovel(Mundo.Venda() with { Preco = 1_500 });

        Assert.Equal([meio.Codigo],
            await Codigos(new FiltroDoSite { Finalidade = Finalidade.Aluguel, PrecoMinimo = 1_200, PrecoMaximo = 1_800 }));
        Assert.Equal([venda.Codigo], await Codigos(new FiltroDoSite { PrecoMinimo = 1_200, PrecoMaximo = 1_800 }));
    }

    [Fact]
    public async Task Filtra_por_tipo_bairro_quartos_vagas_situacao_e_condicoes()
    {
        var casa = _m.CriarImovel(Mundo.Venda("Casa 3 quartos") with
        {
            Tipo = TipoImovel.Casa, Quartos = 3, Vagas = 2, Mcmv = true, AceitaFgts = true, Situacao = Situacao.NaPlanta,
        }, bairro: "Santa Gema");
        _m.CriarImovel(Mundo.Venda("Casa 2 quartos") with { Tipo = TipoImovel.Casa, Quartos = 2 }, bairro: "Santa Gema");
        _m.CriarImovel(Mundo.Venda("Apartamento 3 quartos") with { Quartos = 3 }, bairro: "Centro");

        Assert.Equal([casa.Codigo], await Codigos(new FiltroDoSite { Tipo = TipoImovel.Casa, Quartos = 3 }));
        Assert.Equal([casa.Codigo], await Codigos(new FiltroDoSite { Bairro = "santa-gema", Vagas = 2 }));
        Assert.Equal([casa.Codigo], await Codigos(new FiltroDoSite { Mcmv = true, AceitaFgts = true }));
        Assert.Equal([casa.Codigo], await Codigos(new FiltroDoSite { Situacao = Situacao.NaPlanta }));
    }

    [Fact]
    public async Task Bairro_que_nao_existe_nao_acha_nada()
    {
        _m.CriarImovel(Mundo.Venda(), bairro: "Centro");

        var lista = Ok(await _m.ImovelDoSite.Pesquisar(new FiltroDoSite { Bairro = "bairro-que-nao-existe" }));

        Assert.Empty(lista.Itens);
        Assert.Equal(0, lista.Total);
    }

    [Fact]
    public async Task Pagina_de_doze_com_o_total_de_encontrados()
    {
        for (var i = 0; i < 13; i++) _m.CriarImovel(Mundo.Venda($"Apartamento número {i}"));

        var primeira = Ok(await _m.ImovelDoSite.Pesquisar(new FiltroDoSite()));
        var segunda = Ok(await _m.ImovelDoSite.Pesquisar(new FiltroDoSite { Pagina = 2 }));

        Assert.Equal((12, 13, 1, 12), (primeira.Itens.Count, primeira.Total, primeira.Pagina, primeira.ItensPorPagina));
        Assert.Equal((1, 13, 2), (segunda.Itens.Count, segunda.Total, segunda.Pagina));
        Assert.DoesNotContain(segunda.Itens[0].Codigo, primeira.Itens.Select(i => i.Codigo));
    }

    [Fact]
    public async Task Ordena_por_preco_com_o_consulte_no_fim()
    {
        var caro = _m.CriarImovel(Mundo.Venda("Apartamento caro") with { Preco = 300_000 });
        var consulte = _m.CriarImovel(Mundo.Venda("Apartamento sem preço") with { Preco = null });
        var barato = _m.CriarImovel(Mundo.Venda("Apartamento barato") with { Preco = 200_000 });

        Assert.Equal([barato.Codigo, caro.Codigo, consulte.Codigo], await Codigos(new FiltroDoSite { Ordem = OrdemDoSite.MenorPreco }));
        Assert.Equal([caro.Codigo, barato.Codigo, consulte.Codigo], await Codigos(new FiltroDoSite { Ordem = OrdemDoSite.MaiorPreco }));
    }

    [Fact]
    public async Task Cartao_traz_capa_bairro_e_endereco_da_pagina()
    {
        var imovel = _m.CriarImovel(Mundo.Venda(), quantasFotos: 2, bairro: "Centro");

        var cartao = Assert.Single(Ok(await _m.ImovelDoSite.Pesquisar(new FiltroDoSite())).Itens);

        Assert.Equal(_m.FotosDe(imovel.Id)[0].Url, cartao.CapaUrl);
        Assert.Equal("Centro", cartao.Bairro);
        Assert.Equal(imovel.Caminho, cartao.Caminho);
    }

    [Theory]
    [InlineData(typeof(CartaoDeImovelResponse))]
    [InlineData(typeof(ImovelDoSiteResponse))]
    [InlineData(typeof(PaginaDoImovelResponse))]
    public void Respostas_do_site_nao_tem_dado_interno(Type tipo)
    {
        var nomes = tipo.GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain(nomes, n => n.Contains("Proprietario") || n.Contains("Endereco") || n.Contains("AtualizadoPor"));
    }

    // --- Página do imóvel ----------------------------------------------------------------------------

    [Fact]
    public async Task Abre_pelo_trecho_da_url_com_a_capa_primeiro_e_sem_o_endereco_exato()
    {
        var imovel = _m.CriarImovel(Mundo.Venda(), quantasFotos: 3, bairro: "Centro");
        Ok(await _m.ImovelService.Salvar(new(imovel.Id, Mundo.Venda() with { BairroId = _m.Bairro("Centro").Id, Endereco = "Rua A, 100" })));
        var fotos = _m.FotosDe(imovel.Id);
        Ok(await _m.ImovelService.SalvarFotos(imovel.Id,
            [.. fotos.Select(f => new FotoDesejada(f.Id, f.Url, f.CaminhoNoStorage, f.Id == fotos[2].Id))]));

        var pagina = Ok(await _m.ImovelDoSite.Obter($"{imovel.Codigo.ToLowerInvariant()}-um-slug-antigo"));

        Assert.Equal(imovel.Codigo, pagina.Imovel.Codigo);
        Assert.Equal([fotos[2].Url, fotos[0].Url, fotos[1].Url], pagina.Imovel.Fotos.Select(f => f.Url));
        Assert.Equal("Centro", pagina.Imovel.Bairro!.Nome);
        Assert.DoesNotContain("Rua A", System.Text.Json.JsonSerializer.Serialize(pagina));
    }

    [Theory]
    [InlineData(StatusImovel.Rascunho)]
    [InlineData(StatusImovel.Pausado)]
    public async Task Rascunho_e_pausado_nao_abrem_no_site(StatusImovel status)
    {
        var imovel = _m.CriarImovel(Mundo.Venda(status: status));

        var r = await _m.ImovelDoSite.Obter(imovel.Codigo);

        Assert.Equal(TipoFalha.NaoEncontrado, r.Falha);
        Assert.Equal(["Imóvel não encontrado."], r.Mensagens);
    }

    [Fact]
    public async Task Vendido_abre_com_o_selo_ate_o_prazo_acabar()
    {
        var vendido = _m.CriarImovel(Mundo.Venda(status: StatusImovel.Vendido));

        Assert.Equal(StatusImovel.Vendido, Ok(await _m.ImovelDoSite.Obter(vendido.Codigo)).Imovel.Status);

        _m.Avancar(TimeSpan.FromDays(31));
        Assert.Equal(TipoFalha.NaoEncontrado, (await _m.ImovelDoSite.Obter(vendido.Codigo)).Falha);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("nao-existe")]
    public async Task Trecho_sem_imovel_responde_nao_encontrado(string trecho)
    {
        Assert.Equal(TipoFalha.NaoEncontrado, (await _m.ImovelDoSite.Obter(trecho)).Falha);
    }

    [Fact]
    public async Task Semelhantes_sao_ativos_da_mesma_finalidade_com_tipo_ou_bairro_em_comum()
    {
        var imovel = _m.CriarImovel(Mundo.Venda(), bairro: "Centro");
        var mesmoBairro = _m.CriarImovel(Mundo.Venda("Casa no Centro") with { Tipo = TipoImovel.Casa }, bairro: "Centro");
        var mesmoTipo = _m.CriarImovel(Mundo.Venda("Apartamento na Matriz"), bairro: "Matriz");
        _m.CriarImovel(Mundo.Venda("Terreno na Matriz") with { Tipo = TipoImovel.Terreno }, bairro: "Matriz");
        _m.CriarImovel(Mundo.Venda("Apartamento vendido", StatusImovel.Vendido), bairro: "Centro");
        _m.CriarImovel(Aluguel(1_500), bairro: "Centro");

        var semelhantes = Ok(await _m.ImovelDoSite.Obter(imovel.Codigo)).Semelhantes;

        Assert.Equal([mesmoBairro.Codigo, mesmoTipo.Codigo], semelhantes.Select(s => s.Codigo).Order());
    }

    // --- Home e sitemap ------------------------------------------------------------------------------

    [Fact]
    public async Task Destaques_aluguel_e_mcmv_da_home_mostram_so_ativos()
    {
        var destaque = _m.CriarImovel(Mundo.Venda("Destaque ativo") with { Destaque = true, Mcmv = true });
        _m.CriarImovel(Mundo.Venda("Destaque vendido", StatusImovel.Vendido) with { Destaque = true, Mcmv = true });
        _m.CriarImovel(Mundo.Venda("Destaque rascunho", StatusImovel.Rascunho) with { Destaque = true });
        var aluguel = _m.CriarImovel(Aluguel(1_200));
        _m.CriarImovel(Aluguel(1_300) with { Status = StatusImovel.Alugado });

        Assert.Equal([destaque.Codigo], Ok(await _m.ImovelDoSite.Destaques()).Select(i => i.Codigo));
        Assert.Equal([aluguel.Codigo], Ok(await _m.ImovelDoSite.AlugueisRecentes()).Select(i => i.Codigo));
        Assert.Equal([destaque.Codigo], Ok(await _m.ImovelDoSite.DoMcmv()).Select(i => i.Codigo));
    }

    [Fact]
    public async Task Sitemap_lista_so_os_ativos_com_o_endereco_da_pagina()
    {
        var ativo = _m.CriarImovel(Mundo.Venda());
        _m.CriarImovel(Mundo.Venda("Vendido", StatusImovel.Vendido));
        _m.CriarImovel(Mundo.Venda("Rascunho", StatusImovel.Rascunho));

        var item = Assert.Single(Ok(await _m.ImovelDoSite.Sitemap()));

        Assert.Equal(ativo.Caminho, item.Caminho);
    }

    // --- Prévia no painel ----------------------------------------------------------------------------

    [Fact]
    public async Task Previa_do_painel_mostra_o_rascunho_como_ficaria_no_site()
    {
        var rascunho = _m.CriarImovel(Mundo.Venda(status: StatusImovel.Rascunho), quantasFotos: 1, bairro: "Matriz");

        var previa = Ok(await _m.ImovelService.Previa(rascunho.Id));

        Assert.Equal(StatusImovel.Rascunho, previa.Status);
        Assert.Equal("Matriz", previa.Bairro!.Nome);
        Assert.Single(previa.Fotos);
    }
}
