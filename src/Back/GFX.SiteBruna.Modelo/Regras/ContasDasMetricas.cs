namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Contas do início do painel.</summary>
public static class ContasDasMetricas
{
    /// <summary>Cliques no WhatsApp ÷ visualizações, em %: mostra quais imóveis convertem e quais só recebem visitas.</summary>
    public static int TaxaDeInteresse(int cliques, int visualizacoes) =>
        visualizacoes == 0 ? 0 : (int)Math.Round(cliques * 100m / visualizacoes, MidpointRounding.AwayFromZero);

    /// <summary>Quanto mudou em relação ao período anterior, em %. Sem base de comparação, <c>null</c>.</summary>
    public static int? Variacao(int atual, int anterior) =>
        anterior == 0 ? null : (int)Math.Round((atual - anterior) * 100m / anterior, MidpointRounding.AwayFromZero);
}
