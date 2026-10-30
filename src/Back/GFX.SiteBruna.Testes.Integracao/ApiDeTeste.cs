using GFX.SiteBruna.Dados.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>A Api de verdade (Program.cs inteiro) em memória, com Postgres descartável e WhatsApp em modo simulado.</summary>
public sealed class ApiDeTeste : IAsyncLifetime
{
    public const string AppSecret = "segredo-do-app-de-teste";
    public const string VerifyToken = "token-de-verificacao-de-teste";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private WebApplicationFactory<Program> _fabrica = null!;

    public HttpClient Cliente { get; private set; } = null!;
    public IServiceProvider Servicos => _fabrica.Services;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var configuracao = new Dictionary<string, string>
        {
            ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
            ["Jwt:Key"] = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)),
            ["Jwt:Issuer"] = "site-bruna-teste",
            ["Jwt:Audience"] = "painel-teste",
            ["WhatsApp:Habilitado"] = "true",
            ["WhatsApp:Modo"] = "simulado",
            ["WhatsApp:AppSecret"] = AppSecret,
            ["WhatsApp:VerifyToken"] = VerifyToken,
            ["Site:UrlPublica"] = "https://site.teste",
        };
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testes");
            foreach (var (chave, valor) in configuracao) b.UseSetting(chave, valor);
        });
        Cliente = _fabrica.CreateClient();

        await using var escopo = Servicos.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        await _container.DisposeAsync();
    }
}
