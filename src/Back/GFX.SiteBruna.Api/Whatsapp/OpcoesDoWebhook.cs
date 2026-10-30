namespace GFX.SiteBruna.Api.Whatsapp;

/// <summary>
/// Parte da seção <c>WhatsApp</c> que a Api usa para conferir quem chama o webhook. Segredos (App Secret, Verify Token)
/// só por user secrets ou variáveis de ambiente.
/// </summary>
public sealed class OpcoesDoWebhook
{
    public bool Habilitado { get; set; }
    public string Modo { get; set; } = "meta";
    public string Token { get; set; } = "";
    public string PhoneNumberId { get; set; } = "";
    /// <summary>Chave secreta do app na Meta: assina o corpo de cada chamada (X-Hub-Signature-256).</summary>
    public string AppSecret { get; set; } = "";
    /// <summary>Texto aleatório combinado com a Meta na verificação do webhook.</summary>
    public string VerifyToken { get; set; } = "";

    /// <summary>O que falta configurar (vazio = pronto). O modo simulado dispensa token e número da Meta.</summary>
    public IReadOnlyList<string> Pendencias()
    {
        List<string> faltando = [];
        if (AppSecret.Length < 16) faltando.Add("WhatsApp:AppSecret");
        if (VerifyToken.Length < 16) faltando.Add("WhatsApp:VerifyToken");
        if (Modo != "simulado" && Token.Length == 0) faltando.Add("WhatsApp:Token");
        if (Modo != "simulado" && PhoneNumberId.Length == 0) faltando.Add("WhatsApp:PhoneNumberId");
        return faltando;
    }
}
