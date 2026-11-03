using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>"Imóveis semelhantes" no fim da página do imóvel.</summary>
public static class ImoveisSemelhantes
{
    public const int Quantidade = 4;

    /// <summary>
    /// Mesma finalidade; mesmo tipo e mesmo bairro valem um ponto cada, e quem não tem nenhum dos dois fica de fora.
    /// Mais pontos primeiro; no empate, mantém a ordem dos candidatos (os mais recentes vêm antes).
    /// </summary>
    public static IReadOnlyList<Imovel> Escolher(Imovel referencia, IEnumerable<Imovel> candidatos, int quantidade = Quantidade) =>
    [
        .. candidatos
            .Where(c => c.Id != referencia.Id && c.Finalidade == referencia.Finalidade)
            .Select(c => (Imovel: c, Pontos: Pontos(referencia, c)))
            .Where(c => c.Pontos > 0)
            .OrderByDescending(c => c.Pontos)
            .Select(c => c.Imovel)
            .Take(quantidade),
    ];

    private static int Pontos(Imovel referencia, Imovel candidato) =>
        (candidato.Tipo == referencia.Tipo ? 1 : 0)
        + (candidato.BairroId is { } bairro && bairro == referencia.BairroId ? 1 : 0);
}
