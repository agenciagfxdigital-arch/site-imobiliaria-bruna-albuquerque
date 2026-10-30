using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GFX.SiteBruna.Dados.Data;

internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private const string ViolacaoDeUnicidade = "23505";

    public async Task Commit(CancellationToken ct = default)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: ViolacaoDeUnicidade } pg
                                          && pg.ConstraintName?.Contains("codigo") == true)
        {
            throw new DomainException(CodigoErro.CodigoDuplicado, "Já existe um imóvel com esse código.");
        }
    }

    public void Descartar() => context.ChangeTracker.Clear();
}
