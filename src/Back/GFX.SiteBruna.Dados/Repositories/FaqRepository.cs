using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class FaqRepository(AppDbContext context) : IFaqRepository
{
    public Task<List<Faq>> GetAll() => context.Faq.OrderBy(f => f.Ordem).ThenBy(f => f.CriadoEm).ToListAsync();

    public Task<Faq?> GetById(Guid id) => context.Faq.FirstOrDefaultAsync(f => f.Id == id);

    public void Add(Faq faq) => context.Faq.Add(faq);

    public void Delete(Faq faq) => context.Faq.Remove(faq);
}
