using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IUsuarioRepository
{
    /// <summary>Rastreado. O e-mail chega normalizado (minúsculas).</summary>
    Task<Usuario?> GetByEmail(string email);

    /// <summary>Rastreado.</summary>
    Task<Usuario?> GetById(Guid id);

    Task<IReadOnlyList<Usuario>> GetAll();

    void Add(Usuario usuario);
}
