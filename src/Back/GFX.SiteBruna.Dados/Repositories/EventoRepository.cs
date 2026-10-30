using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GFX.SiteBruna.Dados.Repositories;

/// <summary>Contexto próprio e gravação na hora: um evento ignorado não pode atrapalhar nada da requisição.</summary>
internal sealed class EventoRepository(IDbContextFactory<AppDbContext> fabrica) : IEventoRepository
{
    private const string ViolacaoDeUnicidade = "23505";
    private const string ViolacaoDeChaveEstrangeira = "23503";

    public async Task<bool> Registrar(Evento evento)
    {
        await using var context = await fabrica.CreateDbContextAsync();
        context.Evento.Add(evento);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
                                          { SqlState: ViolacaoDeUnicidade or ViolacaoDeChaveEstrangeira })
        {
            return false; // mesmo imóvel visto de novo pelo mesmo visitante, ou imóvel que não existe mais
        }
    }
}
