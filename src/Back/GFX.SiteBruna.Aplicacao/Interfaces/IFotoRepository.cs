using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IFotoRepository
{
    /// <summary>Rastreadas, na ordem de exibição.</summary>
    Task<List<FotoImovel>> GetDoImovel(Guid imovelId);

    /// <summary>Dos caminhos informados, quais ainda são usados por alguma foto gravada (cópias compartilham arquivos).</summary>
    Task<IReadOnlySet<string>> CaminhosEmUso(IReadOnlyCollection<string> caminhos);

    void Add(FotoImovel foto);

    void Delete(FotoImovel foto);
}
