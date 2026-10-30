using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class BairroRepository(AppDbContext context) : IBairroRepository
{
    public async Task<IReadOnlyList<Bairro>> GetAll() =>
        await context.Bairro.AsNoTracking().OrderBy(b => b.Nome).ToListAsync();

    public Task<Bairro?> GetById(Guid id) => context.Bairro.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
}
