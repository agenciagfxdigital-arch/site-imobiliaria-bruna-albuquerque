using GFX.SiteBruna.Aplicacao.Services;

namespace GFX.SiteBruna.Api.Whatsapp;

/// <summary>
/// Processa a fila de cada telefone avisado, num escopo novo (como uma requisição). Vários telefones andam em paralelo;
/// para o mesmo operador, a reserva no banco garante um processamento por vez.
/// </summary>
public sealed class ProcessadorDoWhatsapp(FilaDeProcessamento fila, IServiceScopeFactory escopos, ILogger<ProcessadorDoWhatsapp> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var telefone in fila.Ler(ct))
            _ = Task.Run(() => Processar(telefone), ct);
    }

    private async Task Processar(string telefone)
    {
        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            await escopo.ServiceProvider.GetRequiredService<AssistenteService>().ProcessarFila(telefone);
        }
        catch (Exception erro)
        {
            // Mensagens ficam pendentes no banco e são retomadas na próxima mensagem desse telefone.
            logger.LogError(erro, "[whatsapp] erro ao processar a fila");
        }
    }
}
