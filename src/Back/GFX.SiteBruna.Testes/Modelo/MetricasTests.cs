using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class MetricasTests
{
    // 01h em UTC do dia 10 ainda é dia 9 em Videira
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Periodo_padrao_e_de_30_dias_terminando_hoje_em_Videira()
    {
        var periodo = PeriodoDasMetricas.Escolher(null, null, null, Agora);

        Assert.Equal(new DateOnly(2026, 11, 9), periodo.Ate);
        Assert.Equal(30, periodo.QuantidadeDeDias);
        Assert.Equal(new DateTimeOffset(2026, 11, 10, 3, 0, 0, TimeSpan.Zero), periodo.Fim);
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(90, 90)]
    [InlineData(45, 30)]
    public void Atalhos_aceitos_sao_7_30_e_90_dias(int pedido, int esperado) =>
        Assert.Equal(esperado, PeriodoDasMetricas.Escolher(pedido, null, null, Agora).QuantidadeDeDias);

    [Fact]
    public void Datas_em_ordem_valem_e_invertidas_voltam_para_o_padrao()
    {
        var (de, ate) = (new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3));

        var escolhido = PeriodoDasMetricas.Escolher(null, de, ate, Agora);
        Assert.Equal(3, escolhido.QuantidadeDeDias);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 3, 0, 0, TimeSpan.Zero), escolhido.Inicio);
        Assert.Equal(new DateTimeOffset(2026, 9, 28, 3, 0, 0, TimeSpan.Zero), escolhido.InicioDoAnterior);
        Assert.Equal(de, escolhido.Dias.First());
        Assert.Equal(ate, escolhido.Dias.Last());

        Assert.Equal(30, PeriodoDasMetricas.Escolher(null, ate, de, Agora).QuantidadeDeDias);
    }

    [Fact]
    public void Periodo_maior_que_dois_anos_e_recusado() =>
        Assert.Throws<DomainException>(() =>
            PeriodoDasMetricas.Escolher(null, new DateOnly(2020, 1, 1), new DateOnly(2026, 1, 1), Agora));

    [Theory]
    [InlineData(1, 8, 13)]
    [InlineData(3, 0, 0)]
    [InlineData(0, 5, 0)]
    public void Taxa_de_interesse_e_cliques_sobre_visualizacoes(int cliques, int visualizacoes, int esperado) =>
        Assert.Equal(esperado, ContasDasMetricas.TaxaDeInteresse(cliques, visualizacoes));

    [Theory]
    [InlineData(150, 100, 50)]
    [InlineData(50, 100, -50)]
    [InlineData(100, 100, 0)]
    public void Variacao_compara_com_o_periodo_anterior(int atual, int anterior, int esperado) =>
        Assert.Equal(esperado, ContasDasMetricas.Variacao(atual, anterior));

    [Fact]
    public void Sem_periodo_anterior_nao_ha_variacao() => Assert.Null(ContasDasMetricas.Variacao(10, 0));
}
