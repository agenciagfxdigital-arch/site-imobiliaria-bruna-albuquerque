namespace GFX.SiteBruna.Front.Web.DTOs.Site;

public sealed record ListaDeImoveisResponse(IReadOnlyList<CartaoDeImovelResponse> Itens, int Total, int Pagina, int ItensPorPagina);
