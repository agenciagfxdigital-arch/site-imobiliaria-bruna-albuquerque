using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class OperadorRepository(AppDbContext context) : IOperadorRepository
{
    public Task<Operador?> GetByTelefone(string telefone) => context.Operador.FirstOrDefaultAsync(o => o.Telefone == telefone);

    public async Task<IReadOnlyList<Operador>> GetAll() =>
        await context.Operador.AsNoTracking().OrderBy(o => o.Nome).ToListAsync();

    public void Add(Operador operador) => context.Operador.Add(operador);

    /// <summary>Mesma condição de <see cref="Operador.PodeSerReservadoPor"/>, num UPDATE atômico.</summary>
    public async Task<bool> Reservar(Guid operadorId, string token, DateTimeOffset ate, DateTimeOffset agora) =>
        await context.Operador
            .Where(o => o.Id == operadorId
                        && (o.ProcessandoAte == null || o.ProcessandoAte < agora || o.ProcessandoToken == token))
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.ProcessandoToken, token)
                .SetProperty(o => o.ProcessandoAte, ate)) > 0;

    public Task Liberar(Guid operadorId, string token) =>
        context.Operador
            .Where(o => o.Id == operadorId && o.ProcessandoToken == token)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.ProcessandoToken, (string?)null)
                .SetProperty(o => o.ProcessandoAte, (DateTimeOffset?)null));
}
