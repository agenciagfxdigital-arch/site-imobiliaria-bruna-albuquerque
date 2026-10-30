using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class ImovelRepository(AppDbContext context) : IImovelRepository
{
    public Task<Imovel?> GetById(Guid id) => context.Imovel.FirstOrDefaultAsync(i => i.Id == id);

    public Task<Imovel?> GetByCodigo(string codigo) => context.Imovel.FirstOrDefaultAsync(i => i.Codigo == codigo);

    public async Task<IReadOnlyList<string>> GetCodigosRecentes(int limite) =>
        await context.Imovel.AsNoTracking().OrderByDescending(i => i.CriadoEm).Take(limite).Select(i => i.Codigo).ToListAsync();

    public Task<bool> ExistsByCodigo(string codigo, Guid? excetoId) =>
        context.Imovel.AnyAsync(i => i.Codigo == codigo && i.Id != excetoId);

    public async Task<IReadOnlyList<Imovel>> Pesquisar(FiltroDeImoveis filtro)
    {
        var consulta = context.Imovel.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = $"%{filtro.Busca.Trim()}%";
            consulta = consulta.Where(i => EF.Functions.ILike(i.Codigo, termo) || EF.Functions.ILike(i.Titulo, termo));
        }
        if (filtro.Status is { } status) consulta = consulta.Where(i => i.Status == status);
        if (filtro.Finalidade is { } finalidade) consulta = consulta.Where(i => i.Finalidade == finalidade);
        if (filtro.BairroId is { } bairroId) consulta = consulta.Where(i => i.BairroId == bairroId);
        return await consulta.OrderByDescending(i => i.AtualizadoEm).Take(filtro.Limite).ToListAsync();
    }

    public async Task<string> ProximoCodigo()
    {
        var numero = await context.Database
            .SqlQueryRaw<long>($"select nextval('{AppDbContext.Schema}.{AppDbContext.SequenciaDeCodigo}') as \"Value\"")
            .SingleAsync();
        return $"BA{numero}";
    }

    public void Add(Imovel imovel) => context.Imovel.Add(imovel);

    public void Delete(Imovel imovel) => context.Imovel.Remove(imovel);
}
