namespace GFX.SiteBruna.Front.Web.DTOs.Metricas;

public sealed record MetricasResponse(
    DateOnly De,
    DateOnly Ate,
    IndicadorResponse Visitantes,
    IndicadorResponse VisualizacoesDeImoveis,
    IndicadorResponse CliquesNoWhatsapp,
    List<TotalDoDia> CliquesPorDia,
    List<ImovelNasMetricasResponse> Imoveis,
    List<TotalDoFiltro> Bairros,
    List<TotalDoFiltro> Tipos,
    List<TotalDoFiltro> Faixas,
    int BuscasDeCompra,
    int BuscasDeAluguel);
