namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>O que o painel envia em Configurações. Quem valida é <c>Configuracoes</c>.</summary>
public sealed record DadosDasConfiguracoes
{
    public string Whatsapp { get; init; } = "";
    public string MensagemPadrao { get; init; } = "";
    public string Instagram { get; init; } = "";
    public string Facebook { get; init; } = "";
    public string Email { get; init; } = "";
    public string Creci { get; init; } = "";
    public string Cidade { get; init; } = "";
    public string Ga4Id { get; init; } = "";
    public string MetaPixelId { get; init; } = "";
    public int DiasSeloVendido { get; init; }
}
