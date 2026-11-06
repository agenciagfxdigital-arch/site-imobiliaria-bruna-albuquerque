namespace GFX.SiteBruna.Aplicacao.DTOs.Metricas;

public sealed record MetricasResponse(
    DateOnly De,
    DateOnly Ate,
    IndicadorResponse Visitantes,
    IndicadorResponse VisualizacoesDeImoveis,
    IndicadorResponse CliquesNoWhatsapp,
    IReadOnlyList<TotalDoDia> CliquesPorDia,
    IReadOnlyList<ImovelNasMetricasResponse> Imoveis,
    IReadOnlyList<TotalDoFiltro> Bairros,
    IReadOnlyList<TotalDoFiltro> Tipos,
    IReadOnlyList<TotalDoFiltro> Faixas,
    int BuscasDeCompra,
    int BuscasDeAluguel);
