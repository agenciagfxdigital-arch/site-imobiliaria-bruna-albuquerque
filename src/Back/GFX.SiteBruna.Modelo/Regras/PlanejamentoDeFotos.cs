using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Regras da lista de fotos de um imóvel.</summary>
public static class PlanejamentoDeFotos
{
    public const int MaximoPorImovel = 60;

    /// <summary>
    /// Compara as fotos gravadas com a lista desejada e decide o que fazer:
    /// a ordem final é a ordem da lista; há sempre exatamente uma capa (a primeira marcada, ou a primeira foto);
    /// uma foto com <c>Id</c> de outro imóvel é recusada.
    /// </summary>
    public static PlanoDeFotos Planejar(IReadOnlyList<FotoImovel> atuais, IReadOnlyList<FotoDesejada> desejadas)
    {
        if (desejadas.Count > MaximoPorImovel)
            throw DomainException.Validacao($"Fotos inválidas (máximo de {MaximoPorImovel} por imóvel).");
        if (desejadas.Any(f => string.IsNullOrWhiteSpace(f.Url) || f.Url.Length > 1000 || f.CaminhoNoStorage?.Length > 500))
            throw DomainException.Validacao($"Fotos inválidas (máximo de {MaximoPorImovel} por imóvel).");

        var idsAtuais = atuais.Select(f => f.Id).ToHashSet();
        if (desejadas.Any(f => f.Id is { } id && !idsAtuais.Contains(id)))
            throw DomainException.Validacao("Uma das fotos não pertence a este imóvel. Recarregue a página.");

        var mantidas = desejadas.Where(f => f.Id is not null).Select(f => f.Id!.Value).ToHashSet();
        var indiceCapa = Math.Max(0, desejadas.ToList().FindIndex(f => f.Capa));

        return new PlanoDeFotos(
            [.. atuais.Where(f => !mantidas.Contains(f.Id))],
            [.. desejadas.Select((f, ordem) => new FotoPlanejada(f.Id, f.Url, f.CaminhoNoStorage, ordem, ordem == indiceCapa))]);
    }

    /// <summary>
    /// Arquivos que podem sair do storage: os das fotos removidas que nenhuma outra foto usa
    /// (cópias de imóvel compartilham os mesmos arquivos).
    /// </summary>
    public static IReadOnlyList<string> ArquivosParaApagar(IEnumerable<string?> caminhosRemovidos, IReadOnlySet<string> caminhosAindaEmUso) =>
        [.. caminhosRemovidos.OfType<string>().Where(c => c.Length > 0).Distinct().Where(c => !caminhosAindaEmUso.Contains(c))];
}
