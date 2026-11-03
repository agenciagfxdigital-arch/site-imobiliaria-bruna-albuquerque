namespace GFX.SiteBruna.Aplicacao.DTOs.Site;

/// <summary>Uma página da lista do site. <c>Pagina</c> é a que foi atendida (o filtro normalizado).</summary>
public sealed record ListaDeImoveisResponse(IReadOnlyList<CartaoDeImovelResponse> Itens, int Total, int Pagina, int ItensPorPagina);
