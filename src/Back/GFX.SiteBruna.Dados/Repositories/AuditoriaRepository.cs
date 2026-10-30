using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class AuditoriaRepository(AppDbContext context) : IAuditoriaRepository
{
    public void Add(RegistroDeAuditoria registro) => context.RegistroDeAuditoria.Add(registro);

    public Task<RegistroDeAuditoria?> GetUltimaConfirmada(Guid operadorId) =>
        context.RegistroDeAuditoria.AsNoTracking()
            .Where(r => r.OperadorId == operadorId && r.Operacao == RegistroDeAuditoria.ConfirmarAcao && r.Sucesso)
            .OrderByDescending(r => r.CriadoEm)
            .FirstOrDefaultAsync();
}
