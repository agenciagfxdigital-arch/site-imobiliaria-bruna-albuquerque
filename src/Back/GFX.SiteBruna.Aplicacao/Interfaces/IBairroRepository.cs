using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IBairroRepository
{
    /// <summary>Em ordem alfabética, sem rastrear.</summary>
    Task<IReadOnlyList<Bairro>> GetAll();

    /// <summary>Rastreado (para renomear).</summary>
    Task<Bairro?> GetById(Guid id);

    Task<bool> ExistsBySlug(string slug, Guid? excetoId);

    /// <summary>Quantos imóveis usam o bairro (não dá para excluir enquanto houver).</summary>
    Task<int> ContarImoveis(Guid bairroId);

    void Add(Bairro bairro);

    void Delete(Bairro bairro);
}
