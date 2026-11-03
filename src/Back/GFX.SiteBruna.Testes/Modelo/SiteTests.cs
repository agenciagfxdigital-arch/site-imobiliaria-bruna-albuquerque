using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

/// <summary>Regras do site público: visibilidade, endereço da página, filtros da URL e imóveis semelhantes.</summary>
public class SiteTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static Imovel Novo(
        StatusImovel status = StatusImovel.Ativo, TipoImovel tipo = TipoImovel.Apartamento, Guid? bairroId = null,
        Finalidade finalidade = Finalidade.Venda, string titulo = "Apartamento 2 quartos com sacada", string? bairro = null) =>
        new(new DadosDoImovel
        {
            Finalidade = finalidade, Tipo = tipo, Titulo = titulo, BairroId = bairroId, Status = status,
        }, "BA101", bairro, OrigemImovel.Painel, Agora);

    // --- Visibilidade ------------------------------------------------------------------------------

    public static TheoryData<StatusImovel, int, int> Cenarios() => new()
    {
        { StatusImovel.Ativo, 0, 30 },
        { StatusImovel.Rascunho, 0, 30 },
        { StatusImovel.Pausado, 0, 30 },
        { StatusImovel.Vendido, 29, 30 },
        { StatusImovel.Vendido, 31, 30 },
        { StatusImovel.Alugado, 10, 30 },
        { StatusImovel.Alugado, 0, 0 },
        { StatusImovel.Vendido, 0, -5 },
    };

    [Theory]
    [MemberData(nameof(Cenarios))]
    public void A_expressao_do_banco_concorda_com_a_regra_da_entidade(StatusImovel status, int diasDepois, int diasDoSelo)
    {
        var imovel = Novo(status);
        var quando = Agora.AddDays(diasDepois);

        var pelaExpressao = Imovel.VisivelNoSite(quando, diasDoSelo).Compile()(imovel);

        Assert.Equal(imovel.EstaVisivelNoSite(quando, diasDoSelo), pelaExpressao);
    }

    [Fact]
    public void Vendido_some_do_site_quando_o_selo_vence()
    {
        var vendido = Novo(StatusImovel.Vendido);
        var visivel = Imovel.VisivelNoSite(Agora.AddDays(29), 30).Compile();
        var vencido = Imovel.VisivelNoSite(Agora.AddDays(31), 30).Compile();

        Assert.True(visivel(vendido));
        Assert.False(vencido(vendido));
    }

    // --- Endereço da página ------------------------------------------------------------------------

    [Fact]
    public void Caminho_usa_o_codigo_em_minusculas_e_o_slug_como_no_site_em_Next()
    {
        var imovel = Novo(titulo: "Casa com pátio", bairro: "Santa Gema");

        Assert.Equal("/imovel/ba101-casa-com-patio-santa-gema", imovel.Caminho);
        Assert.Equal("/imovel/ba101-apto", Imovel.CaminhoNoSite("BA101", "apto"));
    }

    [Theory]
    [InlineData("ba101-apartamento-2-quartos-centro", "BA101")]
    [InlineData("BA101", "BA101")]
    [InlineData("ba101-slug-antigo-de-outro-titulo", "BA101")]
    [InlineData(" ba7x ", "BA7X")]
    public void Codigo_sai_do_trecho_da_url(string trecho, string codigo)
    {
        Assert.Equal(codigo, Imovel.CodigoDoTrecho(trecho));
    }

    [Theory]
    [InlineData("")]
    [InlineData("-apartamento")]
    [InlineData("b-apartamento")]
    [InlineData("ba_101-apartamento")]
    [InlineData("abcdefghijklmnopqrstuvwxyz-codigo-longo-demais")]
    public void Trecho_sem_codigo_valido_nao_acha_nada(string trecho)
    {
        Assert.Null(Imovel.CodigoDoTrecho(trecho));
    }

    // --- Filtros da URL ----------------------------------------------------------------------------

    [Fact]
    public void Aluguel_ignora_filtros_que_so_valem_para_a_venda()
    {
        var filtro = new FiltroDoSite
        {
            Finalidade = Finalidade.Aluguel, Mcmv = true, EntradaParcelada = true, AceitaFgts = true, Mobiliado = true, AceitaPet = true,
        }.Normalizado();

        Assert.False(filtro.Mcmv || filtro.EntradaParcelada || filtro.AceitaFgts);
        Assert.True(filtro.Mobiliado && filtro.AceitaPet);
    }

    [Fact]
    public void Venda_ignora_filtros_que_so_valem_para_o_aluguel()
    {
        var filtro = new FiltroDoSite
        {
            Finalidade = Finalidade.Venda, Mcmv = true, EntradaParcelada = true, AceitaFgts = true, Mobiliado = true, AceitaPet = true,
        }.Normalizado();

        Assert.True(filtro.Mcmv && filtro.EntradaParcelada && filtro.AceitaFgts);
        Assert.False(filtro.Mobiliado || filtro.AceitaPet);
    }

    [Fact]
    public void Descarta_valores_zerados_negativos_ou_fora_da_lista()
    {
        var filtro = new FiltroDoSite
        {
            Finalidade = Finalidade.Venda, Tipo = (TipoImovel)99, Situacao = (Situacao)7, Ordem = (OrdemDoSite)5,
            PrecoMinimo = 0, PrecoMaximo = -10, Quartos = 0, Vagas = 500, Bairro = "  ", Pagina = -3,
        }.Normalizado();

        Assert.Null(filtro.Tipo);
        Assert.Null(filtro.Situacao);
        Assert.Equal(OrdemDoSite.Recentes, filtro.Ordem);
        Assert.Null(filtro.PrecoMinimo);
        Assert.Null(filtro.PrecoMaximo);
        Assert.Null(filtro.Quartos);
        Assert.Null(filtro.Vagas);
        Assert.Null(filtro.Bairro);
        Assert.Equal(1, filtro.Pagina);
    }

    [Fact]
    public void Bairro_vira_slug_e_a_pagina_tem_limite()
    {
        var filtro = new FiltroDoSite { Bairro = "Santa Gema", Pagina = 5000 }.Normalizado();

        Assert.Equal("santa-gema", filtro.Bairro);
        Assert.Equal(FiltroDoSite.UltimaPagina, filtro.Pagina);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 12)]
    [InlineData(3, 24)]
    public void Pagina_vira_quantos_pular(int pagina, int pular)
    {
        Assert.Equal(pular, new FiltroDoSite { Pagina = pagina }.Pular);
    }

    // --- Imóveis semelhantes -----------------------------------------------------------------------

    [Fact]
    public void Semelhantes_priorizam_mesmo_tipo_e_bairro_e_deixam_de_fora_quem_nao_tem_nada_em_comum()
    {
        var centro = Guid.NewGuid();
        var outroBairro = Guid.NewGuid();
        var referencia = Novo(tipo: TipoImovel.Apartamento, bairroId: centro);
        var soTipo = Novo(tipo: TipoImovel.Apartamento, bairroId: outroBairro);
        var tipoEBairro = Novo(tipo: TipoImovel.Apartamento, bairroId: centro);
        var soBairro = Novo(tipo: TipoImovel.Casa, bairroId: centro);
        var nada = Novo(tipo: TipoImovel.Terreno, bairroId: outroBairro);
        var aluguel = Novo(tipo: TipoImovel.Apartamento, bairroId: centro, finalidade: Finalidade.Aluguel);

        var escolhidos = ImoveisSemelhantes.Escolher(referencia, [referencia, soTipo, nada, aluguel, soBairro, tipoEBairro]);

        Assert.Equal([tipoEBairro, soTipo, soBairro], escolhidos);
    }

    [Fact]
    public void Semelhantes_sem_bairro_nao_contam_o_bairro_vazio_como_igual()
    {
        var referencia = Novo(tipo: TipoImovel.Casa);
        var semBairroOutroTipo = Novo(tipo: TipoImovel.Terreno);

        Assert.Empty(ImoveisSemelhantes.Escolher(referencia, [semBairroOutroTipo]));
    }

    [Fact]
    public void Semelhantes_respeitam_o_limite_e_a_ordem_dos_candidatos_no_empate()
    {
        var referencia = Novo();
        var candidatos = Enumerable.Range(0, 6).Select(_ => Novo()).ToList();

        Assert.Equal(candidatos.Take(ImoveisSemelhantes.Quantidade), ImoveisSemelhantes.Escolher(referencia, candidatos));
    }
}
