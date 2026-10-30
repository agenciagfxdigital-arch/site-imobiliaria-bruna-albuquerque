using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GFX.SiteBruna.Dados;

/// <summary>
/// Na subida da Api: só avisa no log se faltar aplicar migration. Quem aplica é o dev
/// (<c>dotnet ef database update --connection ...</c>), nunca a Api.
/// </summary>
public static class VerificacaoBanco
{
    public static async Task VerificarBancoAsync(this IServiceProvider services)
    {
        using var escopo = services.CreateScope();
        var context = escopo.ServiceProvider.GetRequiredService<Data.AppDbContext>();
        var logger = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(VerificacaoBanco));
        try
        {
            var pendentes = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendentes.Count > 0)
                logger.LogWarning("Banco sem as migrations: {Migrations}. Aplique antes de usar (veja docs/MIGRATIONS.md).",
                    string.Join(", ", pendentes));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Não consegui conferir as migrations do banco.");
        }
    }
}
