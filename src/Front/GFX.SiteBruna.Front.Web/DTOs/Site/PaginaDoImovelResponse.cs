namespace GFX.SiteBruna.Front.Web.DTOs.Site;

public sealed record PaginaDoImovelResponse(ImovelDoSiteResponse Imovel, IReadOnlyList<CartaoDeImovelResponse> Semelhantes);
