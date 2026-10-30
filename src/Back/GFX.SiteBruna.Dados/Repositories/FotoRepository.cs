using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class FotoRepository(AppDbContext context) : IFotoRepository
{
    public Task<List<FotoImovel>> GetDoImovel(Guid imovelId) =>
        context.FotoImovel.Where(f => f.ImovelId == imovelId).OrderBy(f => f.Ordem).ToListAsync();

    public async Task<IReadOnlyDictionary<Guid, string>> GetCapas(IReadOnlyCollection<Guid> imovelIds) =>
        await context.FotoImovel.AsNoTracking()
            .Where(f => f.Capa && imovelIds.Contains(f.ImovelId))
            .GroupBy(f => f.ImovelId)
            .Select(g => new { ImovelId = g.Key, Url = g.Min(f => f.Url)! })
            .ToDictionaryAsync(x => x.ImovelId, x => x.Url);

    public async Task<IReadOnlySet<string>> CaminhosEmUso(IReadOnlyCollection<string> caminhos) =>
        (await context.FotoImovel.AsNoTracking()
            .Where(f => f.CaminhoNoStorage != null && caminhos.Contains(f.CaminhoNoStorage))
            .Select(f => f.CaminhoNoStorage!)
            .Distinct()
            .ToListAsync())
        .ToHashSet();

    public void Add(FotoImovel foto) => context.FotoImovel.Add(foto);

    public void Delete(FotoImovel foto) => context.FotoImovel.Remove(foto);
}
