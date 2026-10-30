using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Modelo.Valores;

/// <param name="Removidas">Fotos gravadas que saíram da lista (serão apagadas).</param>
/// <param name="Fotos">Lista final, na ordem de exibição, com exatamente uma capa (se houver fotos).</param>
public sealed record PlanoDeFotos(IReadOnlyList<FotoImovel> Removidas, IReadOnlyList<FotoPlanejada> Fotos);
