using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Envio e mídia no WhatsApp. A implementação real é a Cloud API da Meta; há um modo simulado.</summary>
public interface ICanalWhatsapp
{
    Task EnviarTexto(string telefone, string texto);

    Task EnviarBotoes(string telefone, string texto, IReadOnlyList<BotaoDoWhatsapp> botoes);

    /// <summary>Baixa a foto recebida. Lança <c>DomainException</c> de validação se não der para baixar.</summary>
    Task<byte[]> BaixarMidia(string midiaId);
}
