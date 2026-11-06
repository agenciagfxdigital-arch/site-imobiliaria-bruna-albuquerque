namespace GFX.SiteBruna.Aplicacao.DTOs.Metricas;

/// <summary>Linha da consulta: o que aconteceu com um imóvel no período.</summary>
public sealed record ImovelVisto(Guid Id, string Codigo, string Titulo, string Caminho, string? CapaUrl, int Visualizacoes, int CliquesNoWhatsapp);
