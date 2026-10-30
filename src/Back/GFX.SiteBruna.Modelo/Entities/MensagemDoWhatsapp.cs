using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>
/// Mensagem recebida pelo webhook, na fila de processamento. O <c>Wamid</c> é único: reenvios do webhook não entram duas vezes.
/// </summary>
public sealed class MensagemDoWhatsapp
{
    public Guid Id { get; private set; }
    public string Wamid { get; private set; } = "";
    public Guid? OperadorId { get; private set; }
    public string Telefone { get; private set; } = "";
    public ConteudoMensagem Conteudo { get; private set; } = new ConteudoMensagem.Outro("");
    public StatusMensagem Status { get; private set; }
    public string? Erro { get; private set; }
    public DateTimeOffset RecebidaEm { get; private set; }

    private MensagemDoWhatsapp() { } // EF

    private MensagemDoWhatsapp(MensagemRecebida m, Guid? operadorId, StatusMensagem status, string? motivo)
    {
        Id = Guid.CreateVersion7();
        Wamid = m.Wamid;
        OperadorId = operadorId;
        Telefone = m.Telefone;
        Conteudo = m.Conteudo;
        RecebidaEm = m.RecebidaEm;
        Status = status;
        Erro = motivo;
    }

    /// <summary>De operador autorizado e dentro do limite: entra na fila.</summary>
    public static MensagemDoWhatsapp ParaProcessar(MensagemRecebida m, Guid operadorId) =>
        new(m, operadorId, StatusMensagem.Pendente, null);

    /// <summary>Fica registrada, mas não é processada nem respondida.</summary>
    public static MensagemDoWhatsapp Ignorada(MensagemRecebida m, Guid? operadorId, string motivo) =>
        new(m, operadorId, StatusMensagem.Ignorada, motivo);

    public bool EhFoto => Conteudo is ConteudoMensagem.Imagem;

    /// <param name="aviso">Erro de negócio respondido à Bruna (a mensagem foi tratada mesmo assim).</param>
    public void MarcarProcessada(string? aviso = null)
    {
        Status = StatusMensagem.Processada;
        Erro = aviso;
    }

    public void MarcarErro(string erro)
    {
        Status = StatusMensagem.Erro;
        Erro = erro;
    }

    public void Ignorar(string motivo)
    {
        Status = StatusMensagem.Ignorada;
        Erro = motivo;
    }
}
