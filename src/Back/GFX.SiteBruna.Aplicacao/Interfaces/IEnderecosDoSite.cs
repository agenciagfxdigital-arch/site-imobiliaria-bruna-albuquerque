namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Links que o assistente manda para a Bruna (página pública e prévia no painel).</summary>
public interface IEnderecosDoSite
{
    string PaginaDoImovel(string slug, string codigo);

    string PreviaNoPainel(Guid imovelId);
}
