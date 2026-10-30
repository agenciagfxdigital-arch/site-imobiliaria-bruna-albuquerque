using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GFX.SiteBruna.Aplicacao;

public static class DependencyInjection
{
    /// <summary>Registra os serviços (casos de uso). As interfaces de <c>Interfaces/</c> são registradas pelo Dados.</summary>
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<ImagemService>();
        services.AddScoped<ImovelService>();
        services.AddScoped<ConteudoService>();
        services.AddScoped<FaqService>();
        services.AddScoped<DepoimentoService>();
        services.AddScoped<BairroService>();
        services.AddScoped<EventoService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<IntegracaoService>();
        // Construtor internal: a Api recebe o assistente pronto, mas não monta nem chama a integração por fora.
        services.AddScoped(sp => new AssistenteService(
            sp.GetRequiredService<IntegracaoService>(),
            sp.GetRequiredService<ICanalWhatsapp>(),
            sp.GetRequiredService<IFilaDeMensagens>(),
            sp.GetRequiredService<IOperadorRepository>(),
            sp.GetRequiredService<IUnitOfWork>(),
            sp.GetRequiredService<IRelogio>(),
            sp.GetRequiredService<IEspera>(),
            sp.GetRequiredService<ILogger<AssistenteService>>()));
        return services;
    }
}
