using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IAcaoPendenteRepository
{
    /// <summary>Rastreada.</summary>
    Task<AcaoPendente?> GetById(Guid id);

    /// <summary>Pendentes e dentro da validade, mais recentes primeiro.</summary>
    Task<IReadOnlyList<AcaoPendente>> GetPendentes(Guid operadorId, DateTimeOffset agora);

    void Add(AcaoPendente acao);

    /// <summary>
    /// Passa de pendente para confirmada só se ainda estiver pendente e válida (UPDATE atômico, gravado na hora).
    /// Devolve <c>false</c> se outra requisição chegou antes (ex.: dois toques no botão).
    /// </summary>
    Task<bool> ReservarConfirmacao(Guid id, DateTimeOffset agora);

    /// <summary>Volta para pendente quando a execução falhou (permite tentar de novo).</summary>
    Task DevolverParaPendente(Guid id);
}
