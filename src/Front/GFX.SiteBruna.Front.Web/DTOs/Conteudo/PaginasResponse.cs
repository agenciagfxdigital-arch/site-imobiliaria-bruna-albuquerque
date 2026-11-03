namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

public sealed class PaginasResponse
{
    public ConteudoHome Home { get; set; } = new();
    public ConteudoSobre Sobre { get; set; } = new();
    public ConteudoMcmv Mcmv { get; set; } = new();
}
