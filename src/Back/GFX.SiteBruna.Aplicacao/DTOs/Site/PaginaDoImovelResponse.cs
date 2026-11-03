namespace GFX.SiteBruna.Aplicacao.DTOs.Site;

public sealed record PaginaDoImovelResponse(ImovelDoSiteResponse Imovel, IReadOnlyList<CartaoDeImovelResponse> Semelhantes);
