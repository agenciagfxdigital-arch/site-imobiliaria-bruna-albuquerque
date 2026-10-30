using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GFX.SiteBruna.Dados.Repositories;

/// <summary>
/// Usa um contexto próprio e grava na hora: o resultado de cada mensagem não depende do <c>IUnitOfWork</c> do caso de
/// uso (que pode ser descartado depois de um erro).
/// </summary>
internal sealed class FilaDeMensagens(IDbContextFactory<AppDbContext> fabrica) : IFilaDeMensagens
{
    public async Task<bool> Inserir(MensagemDoWhatsapp mensagem)
    {
        await using var context = await fabrica.CreateDbContextAsync();
        context.MensagemDoWhatsapp.Add(mensagem);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        {
            return false; // reenvio do mesmo wamid pelo webhook
        }
    }

    public async Task<MensagemDoWhatsapp?> ProximaPendente(Guid operadorId)
    {
        await using var context = await fabrica.CreateDbContextAsync();
        return await context.MensagemDoWhatsapp.AsNoTracking()
            .Where(m => m.OperadorId == operadorId && m.Status == StatusMensagem.Pendente)
            .OrderBy(m => m.RecebidaEm)
            .FirstOrDefaultAsync();
    }

    public async Task Gravar(MensagemDoWhatsapp mensagem)
    {
        await using var context = await fabrica.CreateDbContextAsync();
        await context.MensagemDoWhatsapp
            .Where(m => m.Id == mensagem.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, mensagem.Status)
                .SetProperty(m => m.Erro, mensagem.Erro));
    }

    public async Task<int> ContarDesde(Guid operadorId, DateTimeOffset desde)
    {
        await using var context = await fabrica.CreateDbContextAsync();
        return await context.MensagemDoWhatsapp.CountAsync(m => m.OperadorId == operadorId && m.RecebidaEm >= desde);
    }
}
