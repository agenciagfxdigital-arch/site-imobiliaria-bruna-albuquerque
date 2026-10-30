using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Proprietário, endereço exato e quem alterou: tabela só do admin (o público lê o imóvel).</summary>
public interface IDadosInternosRepository
{
    /// <summary>Rastreado; <c>null</c> quando o imóvel ainda não tem dados internos.</summary>
    Task<DadosInternosImovel?> GetByImovel(Guid imovelId);

    void Add(DadosInternosImovel dados);
}
