using GFX.SiteBruna.Aplicacao.DTOs.Metricas;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

/// <summary>Em SQL o que o LINQ não traduz: o dia no fuso de Videira e os campos do <c>jsonb</c> dos filtros.</summary>
internal sealed class MetricasRepository(AppDbContext context) : IMetricasRepository
{
    private IQueryable<Evento> Eventos(DateTimeOffset inicio, DateTimeOffset fim)
    {
        var (de, ate) = (inicio.ToUniversalTime(), fim.ToUniversalTime());
        return context.Evento.AsNoTracking().Where(e => e.CriadoEm >= de && e.CriadoEm < ate);
    }

    public async Task<TotaisDoPeriodo> Totais(DateTimeOffset inicio, DateTimeOffset fim)
    {
        var eventos = Eventos(inicio, fim);
        return new TotaisDoPeriodo(
            await eventos.Select(e => e.VisitanteId).Distinct().CountAsync(),
            await eventos.CountAsync(e => e.Tipo == TipoEvento.PropertyView),
            await eventos.CountAsync(e => e.Tipo == TipoEvento.WhatsappClick));
    }

    public async Task<IReadOnlyList<TotalDoDia>> CliquesPorDia(DateTimeOffset inicio, DateTimeOffset fim)
    {
        var (de, ate) = (inicio.ToUniversalTime(), fim.ToUniversalTime());
        var linhas = await context.Database.SqlQuery<LinhaDeTotal<DateOnly>>($"""
            SELECT (criado_em AT TIME ZONE 'America/Sao_Paulo')::date AS chave, count(*)::int AS total
            FROM site.events
            WHERE tipo = 'whatsapp_click' AND criado_em >= {de} AND criado_em < {ate}
            GROUP BY 1
            """).ToListAsync();
        return [.. linhas.Select(l => new TotalDoDia(l.Chave, l.Total))];
    }

    public async Task<IReadOnlyList<ImovelVisto>> ImoveisMaisVistos(DateTimeOffset inicio, DateTimeOffset fim, int limite)
    {
        var totais = await Eventos(inicio, fim)
            .Where(e => e.ImovelId != null)
            .GroupBy(e => e.ImovelId!.Value)
            .Select(g => new
            {
                Id = g.Key,
                Visualizacoes = g.Count(e => e.Tipo == TipoEvento.PropertyView),
                Cliques = g.Count(e => e.Tipo == TipoEvento.WhatsappClick),
            })
            .OrderByDescending(t => t.Visualizacoes).ThenByDescending(t => t.Cliques)
            .Take(limite)
            .ToListAsync();

        var ids = totais.Select(t => t.Id).ToList();
        var imoveis = await context.Imovel.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id);
        var capas = (await context.Set<FotoImovel>().AsNoTracking()
                .Where(f => ids.Contains(f.ImovelId))
                .OrderByDescending(f => f.Capa).ThenBy(f => f.Ordem)
                .Select(f => new { f.ImovelId, f.Url })
                .ToListAsync())
            .GroupBy(f => f.ImovelId)
            .ToDictionary(g => g.Key, g => g.First().Url);

        return
        [
            .. totais.Where(t => imoveis.ContainsKey(t.Id)).Select(t =>
            {
                var imovel = imoveis[t.Id];
                return new ImovelVisto(imovel.Id, imovel.Codigo, imovel.Titulo, imovel.Caminho, capas.GetValueOrDefault(t.Id), t.Visualizacoes, t.Cliques);
            }),
        ];
    }

    public async Task<IReadOnlyList<TotalDoFiltro>> ValoresDoFiltro(DateTimeOffset inicio, DateTimeOffset fim, string campo, int limite)
    {
        var (de, ate) = (inicio.ToUniversalTime(), fim.ToUniversalTime());
        var linhas = await context.Database.SqlQuery<LinhaDeTotal<string>>($"""
            SELECT meta ->> {campo} AS chave, count(*)::int AS total
            FROM site.events
            WHERE tipo = 'filter_used' AND criado_em >= {de} AND criado_em < {ate} AND coalesce(meta ->> {campo}, '') <> ''
            GROUP BY 1
            ORDER BY 2 DESC, 1
            LIMIT {limite}
            """).ToListAsync();
        return [.. linhas.Select(l => new TotalDoFiltro(l.Chave, l.Total))];
    }
}
