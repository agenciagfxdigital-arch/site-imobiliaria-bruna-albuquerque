namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Avisa o site público que os dados mudaram (as páginas em cache são refeitas).</summary>
public interface ICacheDoSite
{
    Task Invalidar();
}
