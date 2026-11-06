using GFX.SiteBruna.Aplicacao.DTOs.Whatsapp;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Respostas guardadas pelo modo simulado, lidas pelo comando <c>simular-whatsapp</c>.</summary>
public interface ISaidaDoWhatsappSimulado
{
    /// <summary>Devolve as mensagens guardadas e esvazia a lista.</summary>
    IReadOnlyList<MensagemSimulada> Retirar();
}
