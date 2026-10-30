using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IDepoimentoRepository
{
    /// <summary>Na ordem de exibição, sem rastrear.</summary>
    Task<IReadOnlyList<Depoimento>> GetAll(bool somenteDaHome);

    /// <summary>Rastreado.</summary>
    Task<Depoimento?> GetById(Guid id);

    void Add(Depoimento depoimento);

    void Delete(Depoimento depoimento);
}
