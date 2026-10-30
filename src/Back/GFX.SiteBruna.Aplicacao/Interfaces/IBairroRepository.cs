using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IBairroRepository
{
    /// <summary>Em ordem alfabética.</summary>
    Task<IReadOnlyList<Bairro>> GetAll();

    Task<Bairro?> GetById(Guid id);
}
