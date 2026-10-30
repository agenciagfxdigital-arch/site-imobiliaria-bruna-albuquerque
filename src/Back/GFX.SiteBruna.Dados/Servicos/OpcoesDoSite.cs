namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>Seções <c>Site</c> e <c>Front</c>: endereço público e como avisar o front que o conteúdo mudou.</summary>
internal sealed class OpcoesDoSite
{
    public string UrlPublica { get; set; } = "";
    /// <summary>Endereço do Front.Web na rede interna (Docker), para invalidar o cache das páginas.</summary>
    public string UrlInternaDoFront { get; set; } = "";
    public string ChaveDoCache { get; set; } = "";
}
