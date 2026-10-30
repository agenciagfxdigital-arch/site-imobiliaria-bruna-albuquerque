using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IConfiguracoesRepository
{
    /// <summary>Rastreada; <c>null</c> enquanto ninguém salvou (o site usa os valores padrão da entidade).</summary>
    Task<Configuracoes?> Get();

    void Add(Configuracoes configuracoes);
}
