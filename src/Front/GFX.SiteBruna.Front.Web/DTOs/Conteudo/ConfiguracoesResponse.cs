namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

/// <summary>
/// Configurações públicas do site (mutável: o painel edita este mesmo objeto). <see cref="Padrao"/> repete os valores
/// padrão do Modelo, para os botões de WhatsApp continuarem certos mesmo se a Api não responder.
/// </summary>
public sealed class ConfiguracoesResponse
{
    public string Whatsapp { get; set; } = "";
    public string MensagemPadrao { get; set; } = "";
    public string Instagram { get; set; } = "";
    public string Facebook { get; set; } = "";
    public string Email { get; set; } = "";
    public string Creci { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Ga4Id { get; set; } = "";
    public string MetaPixelId { get; set; } = "";
    public int DiasSeloVendido { get; set; }

    public static ConfiguracoesResponse Padrao() => new()
    {
        Whatsapp = "5549999304262",
        MensagemPadrao = "Olá, Bruna! Vim pelo site e gostaria de conversar sobre imóveis.",
        Creci = "CRECI/SC 60723",
        Cidade = "Videira/SC",
        DiasSeloVendido = 30,
    };
}
