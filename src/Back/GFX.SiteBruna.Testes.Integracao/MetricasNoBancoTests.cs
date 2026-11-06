using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Aplicacao.DTOs.Metricas;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.Extensions.DependencyInjection;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>As consultas do início do painel no Postgres de verdade (dia no fuso de Videira e campos do jsonb).</summary>
public class MetricasNoBancoTests(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    private static readonly DateTimeOffset Hoje = new(2026, 6, 15, 12, 0, 0, TimeSpan.FromHours(-3));

    private async Task<ImovelSalvoResponse> NovoImovel()
    {
        await using var escopo = banco.Escopo();
        var dados = new DadosDoImovel
        {
            Finalidade = Finalidade.Venda, Tipo = TipoImovel.Apartamento, Titulo = "Apartamento mais visto do Centro", Preco = 300_000,
            Quartos = 2, Vagas = 1, Status = StatusImovel.Ativo,
        };
        return (await escopo.ServiceProvider.GetRequiredService<ImovelService>().Salvar(new(null, dados))).Dados!;
    }

    private async Task Registrar(TipoEvento tipo, DateTimeOffset quando, string visitante = "visitante-1", Guid? imovel = null,
        Dictionary<string, string?>? detalhes = null)
    {
        await using var escopo = banco.Escopo();
        var dados = new DadosDoEvento { Tipo = tipo, ImovelId = imovel, VisitanteId = visitante, Detalhes = detalhes };
        Assert.True(await escopo.ServiceProvider.GetRequiredService<IEventoRepository>().Registrar(new Evento(dados, Dispositivo.Mobile, quando.ToUniversalTime())));
    }

    private async Task<MetricasResponse> Obter(DateOnly de, DateOnly ate)
    {
        await using var escopo = banco.Escopo();
        var resposta = await escopo.ServiceProvider.GetRequiredService<MetricasService>().Obter(null, de, ate);
        Assert.True(resposta.Sucesso, string.Join(" ", resposta.Mensagens));
        return resposta.Dados!;
    }

    [Fact]
    public async Task Soma_o_periodo_compara_com_o_anterior_e_separa_os_dias_no_horario_de_Videira()
    {
        var imovel = await NovoImovel();
        await Registrar(TipoEvento.PageView, Hoje.AddDays(-1), "visitante-1");
        await Registrar(TipoEvento.PageView, Hoje, "visitante-2");
        await Registrar(TipoEvento.PageView, Hoje, "visitante-2");
        await Registrar(TipoEvento.PropertyView, Hoje, "visitante-2", imovel.Id);
        await Registrar(TipoEvento.WhatsappClick, Hoje, "visitante-2", imovel.Id);
        // 23h30 em Videira já é o dia seguinte em UTC: conta no dia 15
        await Registrar(TipoEvento.WhatsappClick, new DateTimeOffset(2026, 6, 15, 23, 30, 0, TimeSpan.FromHours(-3)), "visitante-3", imovel.Id);

        var r = await Obter(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 15));

        Assert.Equal(2, r.Visitantes.Valor);
        Assert.Equal(1, r.Visitantes.Anterior);
        Assert.Equal(100, r.Visitantes.Variacao);
        Assert.Equal(1, r.VisualizacoesDeImoveis.Valor);
        Assert.Equal(2, r.CliquesNoWhatsapp.Valor);
        Assert.Null(r.CliquesNoWhatsapp.Variacao);
        Assert.Equal(new TotalDoDia(new DateOnly(2026, 6, 15), 2), Assert.Single(r.CliquesPorDia));

        var visto = Assert.Single(r.Imoveis);
        Assert.Equal(imovel.Id, visto.Id);
        Assert.Equal((1, 2, 200), (visto.Visualizacoes, visto.CliquesNoWhatsapp, visto.TaxaDeInteresse));
        Assert.StartsWith("/imovel/", visto.Caminho);
    }

    [Fact]
    public async Task Dias_sem_clique_aparecem_com_zero_e_a_capa_vem_do_imovel()
    {
        var quando = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.FromHours(-3));
        var imovel = await NovoImovel();
        await using (var escopo = banco.Escopo())
        {
            await escopo.ServiceProvider.GetRequiredService<ImovelService>().SalvarFotos(imovel.Id,
            [
                new FotoDesejada(null, "/media/a.webp", null, false),
                new FotoDesejada(null, "/media/capa.webp", null, true),
            ]);
        }
        await Registrar(TipoEvento.WhatsappClick, quando, "visitante-4", imovel.Id);

        var r = await Obter(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3));

        Assert.Equal([0, 1, 0], r.CliquesPorDia.Select(d => d.Total));
        Assert.Equal("/media/capa.webp", Assert.Single(r.Imoveis).CapaUrl);
    }

    [Fact]
    public async Task Filtros_mostram_o_que_o_publico_procura()
    {
        var quando = new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.FromHours(-3));
        Dictionary<string, string?> Filtro(string finalidade, string? bairro, string? tipo = null, string? faixa = null) => new()
        {
            ["finalidade"] = finalidade, ["bairro_nome"] = bairro, ["tipo"] = tipo, ["faixa"] = faixa,
        };
        await Registrar(TipoEvento.FilterUsed, quando, detalhes: Filtro("venda", "Centro", "Casa", "Até R$ 300 mil"));
        await Registrar(TipoEvento.FilterUsed, quando, detalhes: Filtro("venda", "Centro"));
        await Registrar(TipoEvento.FilterUsed, quando, detalhes: Filtro("aluguel", "Matriz"));

        var r = await Obter(new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 5));

        Assert.Equal([new TotalDoFiltro("Centro", 2), new TotalDoFiltro("Matriz", 1)], r.Bairros);
        Assert.Equal([new TotalDoFiltro("Casa", 1)], r.Tipos);
        Assert.Equal([new TotalDoFiltro("Até R$ 300 mil", 1)], r.Faixas);
        Assert.Equal((2, 1), (r.BuscasDeCompra, r.BuscasDeAluguel));
    }

    [Fact]
    public async Task Conteudo_inicial_preenche_uma_vez_e_nao_duplica()
    {
        await using var escopo = banco.Escopo();
        var conteudo = escopo.ServiceProvider.GetRequiredService<ConteudoInicialService>();

        var primeira = await conteudo.Carregar();
        var segunda = await conteudo.Carregar();

        Assert.True(primeira.Sucesso, string.Join(" ", primeira.Mensagens));
        Assert.Equal((ConteudoInicial.Bairros.Count, ConteudoInicial.Perguntas.Count, ConteudoInicial.Depoimentos.Count),
            (primeira.Dados!.Bairros, primeira.Dados.Perguntas, primeira.Dados.Depoimentos));
        Assert.Equal((0, 0, 0), (segunda.Dados!.Bairros, segunda.Dados.Perguntas, segunda.Dados.Depoimentos));
        Assert.Equal(ConteudoInicial.Bairros.Count, escopo.ServiceProvider.GetRequiredService<AppDbContext>().Bairro.Count());
    }
}
