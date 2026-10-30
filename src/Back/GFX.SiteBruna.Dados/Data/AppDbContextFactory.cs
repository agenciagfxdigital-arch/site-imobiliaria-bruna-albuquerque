using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GFX.SiteBruna.Dados.Data;

/// <summary>
/// Para as ferramentas do EF (<c>dotnet ef migrations add</c>). Conexão fixa e sem senha: o <c>migrations add</c> não
/// conecta, e o <c>database update</c> só funciona com <c>--connection</c> explícito. Assim nenhuma configuração da
/// máquina escolhe o banco por engano.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=site_bruna_dev;Username=postgres",
                n => n.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options);
}
