using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Aplicacao.DTOs.Site;

/// <summary>
/// Página do imóvel no site: tudo o que é público, fotos com a capa primeiro. Nunca leva proprietário nem endereço
/// exato (o mapa mostra só o bairro).
/// </summary>
public sealed record ImovelDoSiteResponse(
    Guid Id, string Codigo, string Caminho, Finalidade Finalidade, TipoImovel Tipo, StatusImovel Status, string Titulo,
    string Descricao, decimal? Preco, decimal? Aluguel, decimal? Condominio, decimal? Iptu, decimal? EntradaAPartir,
    BairroResponse? Bairro, bool MostrarMapa, int Quartos, int Suites, int Banheiros, int Vagas, decimal? Area,
    string Andar, IReadOnlyList<string> Diferenciais, bool Mcmv, bool EntradaParcelada, bool AceitaFgts,
    Situacao Situacao, string PrevisaoEntrega, bool Mobiliado, bool AceitaPet, IReadOnlyList<string> Garantias,
    string VideoUrl, IReadOnlyList<FotoDoSiteResponse> Fotos, DateTimeOffset CriadoEm, DateTimeOffset AtualizadoEm)
{
    public static ImovelDoSiteResponse De(Imovel i, Bairro? bairro, IEnumerable<FotoImovel> fotos) => new(
        i.Id, i.Codigo, i.Caminho, i.Finalidade, i.Tipo, i.Status, i.Titulo, i.Descricao, i.Preco, i.Aluguel,
        i.Condominio, i.Iptu, i.EntradaAPartir, bairro is null ? null : BairroResponse.De(bairro), i.MostrarMapa,
        i.Quartos, i.Suites, i.Banheiros, i.Vagas, i.Area, i.Andar, [.. i.Diferenciais], i.Mcmv, i.EntradaParcelada,
        i.AceitaFgts, i.Situacao, i.PrevisaoEntrega, i.Mobiliado, i.AceitaPet, [.. i.Garantias], i.VideoUrl,
        [.. fotos.OrderByDescending(f => f.Capa).ThenBy(f => f.Ordem).Select(f => new FotoDoSiteResponse(f.Id, f.Url))],
        i.CriadoEm, i.AtualizadoEm);
}
