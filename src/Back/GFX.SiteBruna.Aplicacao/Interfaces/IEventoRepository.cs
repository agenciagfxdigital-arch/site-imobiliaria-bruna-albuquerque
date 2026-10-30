using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IEventoRepository
{
    /// <summary>
    /// Grava na hora. Devolve <c>false</c> (sem erro) quando o evento é ignorado: visualização repetida do mesmo imóvel
    /// pelo mesmo visitante, ou imóvel que não existe mais.
    /// </summary>
    Task<bool> Registrar(Evento evento);
}
