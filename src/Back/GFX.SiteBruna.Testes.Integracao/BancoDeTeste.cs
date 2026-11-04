using GFX.SiteBruna.Aplicacao;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados;
using GFX.SiteBruna.Dados.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Testcontainers.PostgreSql;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>
/// Postgres descartável (Docker, via Testcontainers) com a migration aplicada, e a Aplicacao + o Dados montados pela
/// mesma injeção de dependência da Api. Nunca usa o banco local do Supabase nem o de produção.
/// WhatsApp, storage e espera são trocados por dublês que gravam o que receberam.
/// </summary>
public sealed class BancoDeTeste : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public ServiceProvider Servicos { get; private set; } = null!;
    public CanalGravado Canal { get; } = new();
    public StorageGravado Storage { get; } = new();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SiteBruna"] = _container.GetConnectionString(),
            ["WhatsApp:Modo"] = "simulado",
            ["Site:UrlPublica"] = "https://site.teste",
            ["JwtDoSite:Key"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)),
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment());
        services.AddAplicacao();
        services.AddDados(configuracao);
        services.AddSingleton<ICanalWhatsapp>(Canal);
        services.AddSingleton<IArmazenamentoDeArquivos>(Storage);
        services.AddSingleton<IEspera, SemEspera>();
        Servicos = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        await using var escopo = Servicos.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Servicos.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>Um escopo por "requisição", como na Api.</summary>
    public AsyncServiceScope Escopo() => Servicos.CreateAsyncScope();

    private sealed class SemEspera : IEspera
    {
        public Task Esperar(TimeSpan tempo) => Task.CompletedTask;
    }
}
