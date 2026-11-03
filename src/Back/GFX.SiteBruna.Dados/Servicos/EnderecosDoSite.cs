using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>Mesmo formato de URL do site em Next (links já divulgados continuam valendo).</summary>
internal sealed class EnderecosDoSite(IOptions<OpcoesDoSite> opcoes) : IEnderecosDoSite
{
    private string Base => opcoes.Value.UrlPublica.TrimEnd('/');

    public string PaginaDoImovel(string slug, string codigo) => $"{Base}{Imovel.CaminhoNoSite(codigo, slug)}";

    public string PreviaNoPainel(Guid imovelId) => $"{Base}/admin/imoveis/{imovelId:D}/preview";
}
