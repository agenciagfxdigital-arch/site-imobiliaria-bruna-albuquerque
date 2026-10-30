using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IFaqRepository
{
    /// <summary>Rastreadas, na ordem de exibição.</summary>
    Task<List<Faq>> GetAll();

    Task<Faq?> GetById(Guid id);

    void Add(Faq faq);

    void Delete(Faq faq);
}
