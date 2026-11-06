using GFX.SiteBruna.Aplicacao.DTOs.Metricas;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Consultas sobre os eventos de navegação. Início inclusivo, fim exclusivo.</summary>
public interface IMetricasRepository
{
    Task<TotaisDoPeriodo> Totais(DateTimeOffset inicio, DateTimeOffset fim);

    /// <summary>Só os dias com clique, no horário de Videira.</summary>
    Task<IReadOnlyList<TotalDoDia>> CliquesPorDia(DateTimeOffset inicio, DateTimeOffset fim);

    /// <summary>Os mais vistos primeiro.</summary>
    Task<IReadOnlyList<ImovelVisto>> ImoveisMaisVistos(DateTimeOffset inicio, DateTimeOffset fim, int limite);

    /// <summary>Valores mais usados de um campo dos filtros do site (ex.: <c>bairro_nome</c>).</summary>
    Task<IReadOnlyList<TotalDoFiltro>> ValoresDoFiltro(DateTimeOffset inicio, DateTimeOffset fim, string campo, int limite);
}
