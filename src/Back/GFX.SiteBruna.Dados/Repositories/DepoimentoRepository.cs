using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class DepoimentoRepository(AppDbContext context) : IDepoimentoRepository
{
    public async Task<IReadOnlyList<Depoimento>> GetAll(bool somenteDaHome) =>
        await context.Depoimento.AsNoTracking()
            .Where(d => !somenteDaHome || d.ExibirNaHome)
            .OrderBy(d => d.Ordem).ThenBy(d => d.CriadoEm)
            .ToListAsync();

    public Task<Depoimento?> GetById(Guid id) => context.Depoimento.FirstOrDefaultAsync(d => d.Id == id);

    public void Add(Depoimento depoimento) => context.Depoimento.Add(depoimento);

    public void Delete(Depoimento depoimento) => context.Depoimento.Remove(depoimento);
}
