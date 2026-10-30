using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Dados.Repositories;
using GFX.SiteBruna.Dados.Servicos;
using GFX.SiteBruna.Modelo.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Dados;

/// <summary>
/// Única porta de entrada do Dados (junto com <see cref="VerificacaoBanco"/>): tudo o mais é <c>internal</c>, então a Api
/// registra a camada mas não consegue usar o banco, um repositório ou um cliente externo (CS0122).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDados(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");

        // Fábrica (para a fila, que grava fora da unidade de trabalho) + um contexto por requisição
        services.AddDbContextFactory<AppDbContext>(o => o
            .UseNpgsql(conexao, n => n.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.Schema))
            .UseSnakeCaseNamingConvention());
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("banco");

        services.Configure<OpcoesDoSupabase>(configuration.GetSection("Supabase"));
        services.Configure<OpcoesDoWhatsapp>(configuration.GetSection("WhatsApp"));
        services.Configure<OpcoesDoSite>(configuration.GetSection("Site"));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IImovelRepository, ImovelRepository>();
        services.AddScoped<IFotoRepository, FotoRepository>();
        services.AddScoped<IDadosInternosRepository, DadosInternosRepository>();
        services.AddScoped<IBairroRepository, BairroRepository>();
        services.AddScoped<IOperadorRepository, OperadorRepository>();
        services.AddScoped<IAcaoPendenteRepository, AcaoPendenteRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IFilaDeMensagens, FilaDeMensagens>();
        services.AddScoped<IConfiguracoesRepository, ConfiguracoesRepository>();
        services.AddScoped<IPaginasRepository, PaginasRepository>();
        services.AddScoped<IFaqRepository, FaqRepository>();
        services.AddScoped<IDepoimentoRepository, DepoimentoRepository>();
        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        services.AddSingleton<IHasherSenha, HasherSenha>();
        services.AddSingleton<ITokenService>(sp => new TokenService(configuration, sp.GetRequiredService<IRelogio>()));
        services.AddSingleton<IRelogio, RelogioDoSistema>();
        services.AddSingleton<IEspera, EsperaReal>();
        services.AddSingleton<IEnderecosDoSite, EnderecosDoSite>();
        services.AddSingleton<IProcessadorDeImagens, ProcessadorSkia>();
        services.AddHttpClient<IArmazenamentoDeArquivos, ArmazenamentoSupabase>();
        services.AddHttpClient<ICacheDoSite, CacheDoSiteViaHttp>(c => c.Timeout = TimeSpan.FromSeconds(5));

        services.AddHttpClient<CanalMeta>(c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddSingleton<CanalSimulado>();
        services.AddScoped<ICanalWhatsapp>(sp => sp.GetRequiredService<IOptions<OpcoesDoWhatsapp>>().Value.Simulado
            ? sp.GetRequiredService<CanalSimulado>()
            : sp.GetRequiredService<CanalMeta>());

        return services;
    }
}
