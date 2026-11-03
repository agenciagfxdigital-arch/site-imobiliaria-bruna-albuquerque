using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Aplicacao.DTOs.Site;

/// <summary>Cartão de imóvel nas listas do site. Nada interno (proprietário, endereço exato) passa por aqui.</summary>
public sealed record CartaoDeImovelResponse(
    Guid Id, string Codigo, string Caminho, string Titulo, Finalidade Finalidade, TipoImovel Tipo, StatusImovel Status,
    decimal? Preco, decimal? Aluguel, decimal? Condominio, decimal? EntradaAPartir, int Quartos, int Banheiros, int Vagas,
    decimal? Area, string? Bairro, string? CapaUrl, bool Mcmv, bool EntradaParcelada, bool AceitaFgts, Situacao Situacao,
    bool Mobiliado, bool AceitaPet)
{
    public static CartaoDeImovelResponse De(Imovel i, string? bairro, string? capaUrl) => new(
        i.Id, i.Codigo, i.Caminho, i.Titulo, i.Finalidade, i.Tipo, i.Status, i.Preco, i.Aluguel, i.Condominio,
        i.EntradaAPartir, i.Quartos, i.Banheiros, i.Vagas, i.Area, bairro, capaUrl, i.Mcmv, i.EntradaParcelada,
        i.AceitaFgts, i.Situacao, i.Mobiliado, i.AceitaPet);
}
