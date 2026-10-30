namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>Seção <c>WhatsApp</c>. A assinatura do webhook (App Secret, Verify Token) é conferida pela Api.</summary>
internal sealed class OpcoesDoWhatsapp
{
    public bool Habilitado { get; set; }
    /// <summary>"meta" (Cloud API) ou "simulado" (nada vai para a Meta; as respostas aparecem no log).</summary>
    public string Modo { get; set; } = "meta";
    public string Token { get; set; } = "";
    public string PhoneNumberId { get; set; } = "";
    public string VersaoGraph { get; set; } = "v23.0";

    public bool Simulado => Modo == "simulado";
}
