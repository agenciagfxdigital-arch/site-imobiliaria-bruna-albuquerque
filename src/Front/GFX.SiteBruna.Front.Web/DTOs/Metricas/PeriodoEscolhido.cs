namespace GFX.SiteBruna.Front.Web.DTOs.Metricas;

/// <summary>Atalho de dias ou intervalo de datas escolhido no início do painel.</summary>
public sealed record PeriodoEscolhido(int? Dias, DateOnly? De = null, DateOnly? Ate = null);
