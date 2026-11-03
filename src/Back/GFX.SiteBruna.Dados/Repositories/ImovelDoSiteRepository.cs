using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class ImovelDoSiteRepository(AppDbContext context) : IImovelDoSiteRepository
{
    private IQueryable<Imovel> Ativos => context.Imovel.AsNoTracking().Where(i => i.Status == StatusImovel.Ativo);

    public async Task<(IReadOnlyList<Imovel> Itens, int Total)> Pesquisar(
        FiltroDoSite filtro, Guid? bairroId, DateTimeOffset agora, int diasSeloVendido)
    {
        var venda = filtro.Finalidade == Finalidade.Venda;
        var consulta = context.Imovel.AsNoTracking()
            .Where(Imovel.VisivelNoSite(agora, diasSeloVendido))
            .Where(i => i.Finalidade == filtro.Finalidade);

        if (filtro.Tipo is { } tipo) consulta = consulta.Where(i => i.Tipo == tipo);
        if (bairroId is { } bairro) consulta = consulta.Where(i => i.BairroId == bairro);
        if (filtro.PrecoMinimo is { } minimo)
            consulta = venda ? consulta.Where(i => i.Preco >= minimo) : consulta.Where(i => i.Aluguel >= minimo);
        if (filtro.PrecoMaximo is { } maximo)
            consulta = venda ? consulta.Where(i => i.Preco <= maximo) : consulta.Where(i => i.Aluguel <= maximo);
        if (filtro.Quartos is { } quartos) consulta = consulta.Where(i => i.Quartos >= quartos);
        if (filtro.Vagas is { } vagas) consulta = consulta.Where(i => i.Vagas >= vagas);
        if (filtro.Situacao is { } situacao) consulta = consulta.Where(i => i.Situacao == situacao);
        if (filtro.Mcmv) consulta = consulta.Where(i => i.Mcmv);
        if (filtro.EntradaParcelada) consulta = consulta.Where(i => i.EntradaParcelada);
        if (filtro.AceitaFgts) consulta = consulta.Where(i => i.AceitaFgts);
        if (filtro.Mobiliado) consulta = consulta.Where(i => i.Mobiliado);
        if (filtro.AceitaPet) consulta = consulta.Where(i => i.AceitaPet);

        var total = await consulta.CountAsync();

        // Ativos (sem data de venda) primeiro; vendidos/alugados com o selo por último, os mais recentes antes.
        // Preço vazio ("Consulte") fica no fim nas ordens por preço.
        var ordenada = consulta.OrderBy(i => i.VendidoEm != null).ThenByDescending(i => i.VendidoEm);
        ordenada = (filtro.Ordem, venda) switch
        {
            (OrdemDoSite.MenorPreco, true) => ordenada.ThenBy(i => i.Preco == null).ThenBy(i => i.Preco),
            (OrdemDoSite.MenorPreco, false) => ordenada.ThenBy(i => i.Aluguel == null).ThenBy(i => i.Aluguel),
            (OrdemDoSite.MaiorPreco, true) => ordenada.ThenBy(i => i.Preco == null).ThenByDescending(i => i.Preco),
            (OrdemDoSite.MaiorPreco, false) => ordenada.ThenBy(i => i.Aluguel == null).ThenByDescending(i => i.Aluguel),
            _ => ordenada,
        };
        var itens = await ordenada.ThenByDescending(i => i.CriadoEm).ThenBy(i => i.Id)
            .Skip(filtro.Pular).Take(FiltroDoSite.ItensPorPagina)
            .ToListAsync();
        return (itens, total);
    }

    public Task<Imovel?> GetVisivelByCodigo(string codigo, DateTimeOffset agora, int diasSeloVendido) =>
        context.Imovel.AsNoTracking().Where(Imovel.VisivelNoSite(agora, diasSeloVendido)).FirstOrDefaultAsync(i => i.Codigo == codigo);

    public async Task<IReadOnlyList<Imovel>> Destaques(int limite) =>
        await Ativos.Where(i => i.Destaque).OrderByDescending(i => i.CriadoEm).Take(limite).ToListAsync();

    public async Task<IReadOnlyList<Imovel>> Recentes(Finalidade? finalidade, bool somenteMcmv, int limite)
    {
        var consulta = Ativos;
        if (finalidade is { } f) consulta = consulta.Where(i => i.Finalidade == f);
        if (somenteMcmv) consulta = consulta.Where(i => i.Mcmv);
        return await consulta.OrderByDescending(i => i.CriadoEm).Take(limite).ToListAsync();
    }

    public async Task<IReadOnlyList<Imovel>> CandidatosASemelhantes(Imovel referencia, int limite)
    {
        var (id, finalidade, tipo, bairroId) = (referencia.Id, referencia.Finalidade, referencia.Tipo, referencia.BairroId);
        return await Ativos
            .Where(i => i.Id != id && i.Finalidade == finalidade && (i.Tipo == tipo || (bairroId != null && i.BairroId == bairroId)))
            .OrderByDescending(i => i.CriadoEm)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<(string Codigo, string Slug, DateTimeOffset AtualizadoEm)>> ParaSitemap(int limite) =>
        (await Ativos.OrderByDescending(i => i.AtualizadoEm).Take(limite)
            .Select(i => new { i.Codigo, i.Slug, i.AtualizadoEm })
            .ToListAsync())
        .Select(i => (i.Codigo, i.Slug, i.AtualizadoEm))
        .ToList();
}
