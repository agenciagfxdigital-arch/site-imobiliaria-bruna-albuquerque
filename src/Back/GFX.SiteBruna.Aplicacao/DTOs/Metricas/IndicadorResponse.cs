using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Aplicacao.DTOs.Metricas;

/// <summary>Número do período, com a comparação com o período anterior (<c>Variacao</c> é <c>null</c> sem base para comparar).</summary>
public sealed record IndicadorResponse(int Valor, int Anterior, int? Variacao)
{
    public static IndicadorResponse De(int valor, int anterior) => new(valor, anterior, ContasDasMetricas.Variacao(valor, anterior));
}
