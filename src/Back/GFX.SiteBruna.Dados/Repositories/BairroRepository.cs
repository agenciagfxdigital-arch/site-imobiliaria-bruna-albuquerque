using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class BairroRepository(AppDbContext context) : IBairroRepository
{
    public async Task<IReadOnlyList<Bairro>> GetAll() =>
        await context.Bairro.AsNoTracking().OrderBy(b => b.Nome).ToListAsync();

    public Task<Bairro?> GetById(Guid id) => context.Bairro.FirstOrDefaultAsync(b => b.Id == id);

    public Task<bool> ExistsBySlug(string slug, Guid? excetoId) =>
        context.Bairro.AnyAsync(b => b.Slug == slug && b.Id != excetoId);

    public Task<int> ContarImoveis(Guid bairroId) => context.Imovel.CountAsync(i => i.BairroId == bairroId);

    public void Add(Bairro bairro) => context.Bairro.Add(bairro);

    public void Delete(Bairro bairro) => context.Bairro.Remove(bairro);
}
