namespace GFX.SiteBruna.Front.Web.DTOs.Site;

public sealed record CartaoDeImovelResponse(
    Guid Id, string Codigo, string Caminho, string Titulo, Finalidade Finalidade, TipoImovel Tipo, StatusImovel Status,
    decimal? Preco, decimal? Aluguel, decimal? Condominio, decimal? EntradaAPartir, int Quartos, int Banheiros, int Vagas,
    decimal? Area, string? Bairro, string? CapaUrl, bool Mcmv, bool EntradaParcelada, bool AceitaFgts, Situacao Situacao,
    bool Mobiliado, bool AceitaPet);
