using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Aplicacao.DTOs.Imoveis;

/// <summary>Linha da lista de imóveis do painel.</summary>
public sealed record ImovelResumoResponse(
    Guid Id, string Codigo, string Titulo, Finalidade Finalidade, TipoImovel Tipo, StatusImovel Status, decimal? Preco,
    decimal? Aluguel, bool Destaque, OrigemImovel Origem, string? CapaUrl, DateTimeOffset AtualizadoEm)
{
    public static ImovelResumoResponse De(Imovel i, string? capaUrl) => new(
        i.Id, i.Codigo, i.Titulo, i.Finalidade, i.Tipo, i.Status, i.Preco, i.Aluguel, i.Destaque, i.Origem, capaUrl, i.AtualizadoEm);
}
