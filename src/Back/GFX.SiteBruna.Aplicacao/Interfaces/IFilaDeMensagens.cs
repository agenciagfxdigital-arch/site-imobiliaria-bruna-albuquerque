using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>
/// Fila das mensagens recebidas: evita processar o mesmo reenvio do webhook duas vezes e mantém a ordem.
/// Grava na hora (fora do <see cref="IUnitOfWork"/>), para o resultado de uma mensagem não depender do que veio depois.
/// </summary>
public interface IFilaDeMensagens
{
    /// <summary>Devolve <c>false</c> se o <c>Wamid</c> já estava na fila.</summary>
    Task<bool> Inserir(MensagemDoWhatsapp mensagem);

    /// <summary>A pendente mais antiga do operador.</summary>
    Task<MensagemDoWhatsapp?> ProximaPendente(Guid operadorId);

    /// <summary>Grava o status e o erro atuais da mensagem.</summary>
    Task Gravar(MensagemDoWhatsapp mensagem);

    Task<int> ContarDesde(Guid operadorId, DateTimeOffset desde);
}
