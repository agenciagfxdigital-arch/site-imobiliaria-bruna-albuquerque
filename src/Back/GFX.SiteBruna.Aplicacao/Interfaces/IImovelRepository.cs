using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IImovelRepository
{
    /// <summary>Rastreado (para alterar).</summary>
    Task<Imovel?> GetById(Guid id);

    /// <summary>Rastreado. O código chega já em maiúsculas.</summary>
    Task<Imovel?> GetByCodigo(string codigo);

    /// <summary>Mais recentes primeiro (para sugerir códigos quando a Bruna digita um que não existe).</summary>
    Task<IReadOnlyList<string>> GetCodigosRecentes(int limite);

    Task<bool> ExistsByCodigo(string codigo, Guid? excetoId);

    /// <summary>Próximo código da sequência (BA101, BA102…).</summary>
    Task<string> ProximoCodigo();

    void Add(Imovel imovel);

    /// <summary>Fotos e dados internos saem junto (cascata no banco).</summary>
    void Delete(Imovel imovel);
}
