namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

public sealed class ConteudoSobre
{
    public string Foto { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Subtitulo { get; set; } = "";
    public string Historia { get; set; } = "";
    public List<Passo> Passos { get; set; } = [];
    public string AnosExperiencia { get; set; } = "";
    public string FamiliasAtendidas { get; set; } = "";
    public List<string> Entregas { get; set; } = [];
    public string VideoUrl { get; set; } = "";
    public string CtaTitulo { get; set; } = "";
}
