namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

public sealed class ConteudoMcmv
{
    public string Titulo { get; set; } = "";
    public string TextoAbertura { get; set; } = "";
    public string Foto { get; set; } = "";
    public string ComoFunciona { get; set; } = "";
    public string EntradaParcelada { get; set; } = "";
    public string Fgts { get; set; } = "";
    public List<Passo> Entenda { get; set; } = [];
    public List<Passo> Etapas { get; set; } = [];
    public List<string> Documentos { get; set; } = [];
    public List<string> DocumentosFgts { get; set; } = [];
    public string DocumentosObs { get; set; } = "";
    public string DicaPlanejamento { get; set; } = "";
    public string Aviso { get; set; } = "";
}
