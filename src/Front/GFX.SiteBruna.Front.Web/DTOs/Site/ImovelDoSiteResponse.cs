using GFX.SiteBruna.Front.Web.DTOs.Conteudo;

namespace GFX.SiteBruna.Front.Web.DTOs.Site;

public sealed record ImovelDoSiteResponse(
    Guid Id, string Codigo, string Caminho, Finalidade Finalidade, TipoImovel Tipo, StatusImovel Status, string Titulo,
    string Descricao, decimal? Preco, decimal? Aluguel, decimal? Condominio, decimal? Iptu, decimal? EntradaAPartir,
    BairroResponse? Bairro, bool MostrarMapa, int Quartos, int Suites, int Banheiros, int Vagas, decimal? Area,
    string Andar, IReadOnlyList<string> Diferenciais, bool Mcmv, bool EntradaParcelada, bool AceitaFgts,
    Situacao Situacao, string PrevisaoEntrega, bool Mobiliado, bool AceitaPet, IReadOnlyList<string> Garantias,
    string VideoUrl, IReadOnlyList<FotoDoSiteResponse> Fotos, DateTimeOffset CriadoEm, DateTimeOffset AtualizadoEm);
