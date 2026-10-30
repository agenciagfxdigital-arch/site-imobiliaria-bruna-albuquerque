using GFX.SiteBruna.Aplicacao.Interfaces;

namespace GFX.SiteBruna.Dados.Servicos;

internal sealed class EsperaReal : IEspera
{
    public Task Esperar(TimeSpan tempo) => Task.Delay(tempo);
}
