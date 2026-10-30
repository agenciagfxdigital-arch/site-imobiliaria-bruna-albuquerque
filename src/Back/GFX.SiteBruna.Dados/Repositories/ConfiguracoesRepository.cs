using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class ConfiguracoesRepository(AppDbContext context) : IConfiguracoesRepository
{
    public Task<Configuracoes?> Get() => context.Configuracoes.FirstOrDefaultAsync(c => c.Id == Configuracoes.IdUnico);

    public void Add(Configuracoes configuracoes) => context.Configuracoes.Add(configuracoes);
}
