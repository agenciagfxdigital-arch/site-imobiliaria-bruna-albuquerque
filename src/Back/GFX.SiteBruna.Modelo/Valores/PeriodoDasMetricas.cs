using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Período do início do painel, em dias inteiros no horário de Videira. Vai de <see cref="De"/> até <see cref="Ate"/>, inclusive.</summary>
public sealed record PeriodoDasMetricas(DateOnly De, DateOnly Ate)
{
    public const int DiasPadrao = 30;
    public const int MaximoDeDias = 731;

    private static readonly TimeSpan Fuso = TimeSpan.FromHours(-3);
    private static readonly int[] DiasDosAtalhos = [7, 30, 90];

    public int QuantidadeDeDias => Ate.DayNumber - De.DayNumber + 1;
    public DateTimeOffset Inicio => MeiaNoite(De);
    /// <summary>Exclusivo: meia-noite do dia seguinte ao último.</summary>
    public DateTimeOffset Fim => MeiaNoite(Ate.AddDays(1));
    /// <summary>Começo do período anterior, de mesma duração, usado na comparação.</summary>
    public DateTimeOffset InicioDoAnterior => Inicio.AddDays(-QuantidadeDeDias);

    public IEnumerable<DateOnly> Dias => Enumerable.Range(0, QuantidadeDeDias).Select(De.AddDays);

    /// <summary>Datas informadas valem quando as duas vêm e estão em ordem; senão, os últimos 7, 30 ou 90 dias.</summary>
    public static PeriodoDasMetricas Escolher(int? dias, DateOnly? de, DateOnly? ate, DateTimeOffset agora)
    {
        if (de is { } inicio && ate is { } fim && inicio <= fim)
        {
            var periodo = new PeriodoDasMetricas(inicio, fim);
            if (periodo.QuantidadeDeDias > MaximoDeDias) throw DomainException.Validacao("Escolha um período de até 2 anos.");
            return periodo;
        }
        var quantidade = dias is { } d && DiasDosAtalhos.Contains(d) ? d : DiasPadrao;
        var hoje = DateOnly.FromDateTime(agora.ToOffset(Fuso).DateTime);
        return new PeriodoDasMetricas(hoje.AddDays(1 - quantidade), hoje);
    }

    private static DateTimeOffset MeiaNoite(DateOnly dia) => new(dia.ToDateTime(TimeOnly.MinValue), Fuso);
}
