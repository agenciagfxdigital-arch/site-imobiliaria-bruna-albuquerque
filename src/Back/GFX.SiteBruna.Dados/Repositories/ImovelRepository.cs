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
