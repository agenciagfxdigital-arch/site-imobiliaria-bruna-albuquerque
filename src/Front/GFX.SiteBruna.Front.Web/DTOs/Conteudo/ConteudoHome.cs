namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

/// <summary>Textos da página inicial (mutável: o painel edita este mesmo objeto).</summary>
public sealed class ConteudoHome
{
    public string HeroTitulo { get; set; } = "";
    public string HeroSubtitulo { get; set; } = "";
    public string HeroFoto { get; set; } = "";
    public List<string> Servicos { get; set; } = [];
    public string PassosTitulo { get; set; } = "";
    public string PassosTexto { get; set; } = "";
    public List<Passo> Passos { get; set; } = [];
    public string McmvTitulo { get; set; } = "";
    public string McmvTexto { get; set; } = "";
    public string BioTitulo { get; set; } = "";
    public string BioTexto { get; set; } = "";
    public string BioFoto { get; set; } = "";
    public string CtaTitulo { get; set; } = "";
    public string CtaTexto { get; set; } = "";
}
