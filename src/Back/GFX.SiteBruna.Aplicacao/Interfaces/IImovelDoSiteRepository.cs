using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>
/// Leituras do site público, sem rastrear. Quem pede o "agora" e os dias do selo filtra pela mesma regra do Modelo
/// (<see cref="Imovel.VisivelNoSite"/>); as listas da home e os semelhantes mostram só imóveis ativos.
/// </summary>
public interface IImovelDoSiteRepository
{
    /// <summary>
    /// Página da lista com filtros (o filtro chega normalizado; <paramref name="bairroId"/> já resolvido a partir do slug).
    /// Ativos primeiro, vendidos/alugados por último (os mais recentes antes); dentro de cada grupo, a ordem pedida.
    /// </summary>
    Task<(IReadOnlyList<Imovel> Itens, int Total)> Pesquisar(FiltroDoSite filtro, Guid? bairroId, DateTimeOffset agora, int diasSeloVendido);

    /// <summary>Ativo ou com o selo de vendido/alugado. O código chega em maiúsculas.</summary>
    Task<Imovel?> GetVisivelByCodigo(string codigo, DateTimeOffset agora, int diasSeloVendido);

    /// <summary>Ativos e marcados como destaque, mais recentes primeiro.</summary>
    Task<IReadOnlyList<Imovel>> Destaques(int limite);

    /// <summary>Ativos, mais recentes primeiro (filtros opcionais).</summary>
    Task<IReadOnlyList<Imovel>> Recentes(Finalidade? finalidade, bool somenteMcmv, int limite);

    /// <summary>Ativos da mesma finalidade com o mesmo tipo ou o mesmo bairro, mais recentes primeiro (sem o próprio).</summary>
    Task<IReadOnlyList<Imovel>> CandidatosASemelhantes(Imovel referencia, int limite);

    /// <summary>Todos os ativos, só com o que o sitemap usa.</summary>
    Task<IReadOnlyList<(string Codigo, string Slug, DateTimeOffset AtualizadoEm)>> ParaSitemap(int limite);
}
