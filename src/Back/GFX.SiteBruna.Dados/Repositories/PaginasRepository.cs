using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class PaginasRepository(AppDbContext context) : IPaginasRepository
{
    public Task<Paginas?> Get() => context.Paginas.FirstOrDefaultAsync(p => p.Id == Paginas.IdUnico);

    public void Add(Paginas paginas) => context.Paginas.Add(paginas);
}
