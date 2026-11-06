using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

public interface IOperadorRepository
{
    /// <summary>Rastreado. O telefone chega normalizado.</summary>
    Task<Operador?> GetByTelefone(string telefone);

    /// <summary>Em ordem alfabética, sem rastrear.</summary>
    Task<IReadOnlyList<Operador>> GetAll();

    void Add(Operador operador);

    /// <summary>
    /// Reserva atômica (UPDATE condicional, gravado na hora, fora do <see cref="IUnitOfWork"/>), com a mesma condição de
    /// <see cref="Operador.PodeSerReservadoPor"/>: duas execuções simultâneas não reservam juntas.
    /// </summary>
    Task<bool> Reservar(Guid operadorId, string token, DateTimeOffset ate, DateTimeOffset agora);

    /// <summary>Libera só se a reserva ainda for deste token (gravado na hora).</summary>
    Task Liberar(Guid operadorId, string token);
}
