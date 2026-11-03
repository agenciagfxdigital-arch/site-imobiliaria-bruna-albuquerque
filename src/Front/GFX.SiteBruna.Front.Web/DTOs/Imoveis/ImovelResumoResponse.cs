namespace GFX.SiteBruna.Front.Web.DTOs.Imoveis;

/// <summary>Linha da lista de imóveis do painel.</summary>
public sealed record ImovelResumoResponse(
    Guid Id, string Codigo, string Titulo, Finalidade Finalidade, TipoImovel Tipo, StatusImovel Status, decimal? Preco,
    decimal? Aluguel, bool Destaque, OrigemImovel Origem, string? CapaUrl, DateTimeOffset AtualizadoEm);
