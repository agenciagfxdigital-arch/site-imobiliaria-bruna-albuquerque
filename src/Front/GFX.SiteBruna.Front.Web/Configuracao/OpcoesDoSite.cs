namespace GFX.SiteBruna.Front.Web.Configuracao;

/// <summary>Seção <c>Site</c> do appsettings (docs/CONFIGURACAO.md).</summary>
public sealed class OpcoesDoSite
{
    /// <summary>Endereço público (links canônicos, sitemap, compartilhamento). Ex.: https://www.brunaalbuquerque.com.br</summary>
    public string UrlPublica { get; set; } = "";
    /// <summary>Mesma chave do <c>Site:ChaveDoCache</c> da Api: só quem a tem pode pedir para refazer as páginas.</summary>
    public string ChaveDoCache { get; set; } = "";
    /// <summary>Quanto tempo uma página do site fica pronta em memória (0 desliga; o padrão é 5, como o ISR do Next).</summary>
    public int CacheEmMinutos { get; set; } = 5;

    public string UrlBase => UrlPublica.TrimEnd('/');
}
