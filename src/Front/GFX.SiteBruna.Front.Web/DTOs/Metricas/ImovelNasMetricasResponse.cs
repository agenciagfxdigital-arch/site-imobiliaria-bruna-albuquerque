namespace GFX.SiteBruna.Front.Web.DTOs.Metricas;

public sealed record ImovelNasMetricasResponse(
    Guid Id, string Codigo, string Titulo, string Caminho, string? CapaUrl, int Visualizacoes, int CliquesNoWhatsapp, int TaxaDeInteresse);
