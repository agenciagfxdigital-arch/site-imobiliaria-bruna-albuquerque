using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IAuditoriaRepository
{
    void Add(RegistroDeAuditoria registro);

    /// <summary>Último registro de ação confirmada (com sucesso) do operador.</summary>
    Task<RegistroDeAuditoria?> GetUltimaConfirmada(Guid operadorId);
}
