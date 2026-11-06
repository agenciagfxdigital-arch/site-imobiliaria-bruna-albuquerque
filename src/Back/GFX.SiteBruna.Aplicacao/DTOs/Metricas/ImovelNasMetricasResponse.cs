using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Aplicacao.DTOs.Metricas;

public sealed record ImovelNasMetricasResponse(
    Guid Id, string Codigo, string Titulo, string Caminho, string? CapaUrl, int Visualizacoes, int CliquesNoWhatsapp, int TaxaDeInteresse)
{
    public static ImovelNasMetricasResponse De(ImovelVisto i) => new(
        i.Id, i.Codigo, i.Titulo, i.Caminho, i.CapaUrl, i.Visualizacoes, i.CliquesNoWhatsapp,
        ContasDasMetricas.TaxaDeInteresse(i.CliquesNoWhatsapp, i.Visualizacoes));
}
