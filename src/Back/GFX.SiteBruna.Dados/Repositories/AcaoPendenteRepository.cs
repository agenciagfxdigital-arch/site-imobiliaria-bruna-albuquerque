using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class AcaoPendenteRepository(AppDbContext context) : IAcaoPendenteRepository
{
    public Task<AcaoPendente?> GetById(Guid id) => context.AcaoPendente.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<AcaoPendente>> GetPendentes(Guid operadorId, DateTimeOffset agora) =>
        await context.AcaoPendente
            .Where(a => a.OperadorId == operadorId && a.Status == StatusAcao.Pendente && a.ExpiraEm > agora)
            .OrderByDescending(a => a.CriadoEm)
            .ToListAsync();

    public void Add(AcaoPendente acao) => context.AcaoPendente.Add(acao);

    public async Task<bool> ReservarConfirmacao(Guid id, DateTimeOffset agora) =>
        await context.AcaoPendente
            .Where(a => a.Id == id && a.Status == StatusAcao.Pendente && a.ExpiraEm > agora)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, StatusAcao.Confirmada)) > 0;

    public Task DevolverParaPendente(Guid id) =>
        context.AcaoPendente
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, StatusAcao.Pendente));
}
