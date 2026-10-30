using GFX.SiteBruna.Aplicacao.Interfaces;

namespace GFX.SiteBruna.Dados.Servicos;

internal sealed class RelogioDoSistema : IRelogio
{
    public DateTimeOffset Agora => DateTimeOffset.UtcNow;
}
