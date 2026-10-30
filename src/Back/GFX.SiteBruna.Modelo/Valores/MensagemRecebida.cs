namespace GFX.SiteBruna.Modelo.Valores;

/// <param name="Wamid">Id da mensagem no WhatsApp (o mesmo em reenvios do webhook).</param>
public sealed record MensagemRecebida(string Wamid, string Telefone, DateTimeOffset RecebidaEm, ConteudoMensagem Conteudo);
