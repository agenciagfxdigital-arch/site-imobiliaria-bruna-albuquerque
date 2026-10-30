using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IPaginasRepository
{
    /// <summary>Rastreada; <c>null</c> enquanto nenhuma página foi salva (o site usa os textos padrão).</summary>
    Task<Paginas?> Get();

    void Add(Paginas paginas);
}
