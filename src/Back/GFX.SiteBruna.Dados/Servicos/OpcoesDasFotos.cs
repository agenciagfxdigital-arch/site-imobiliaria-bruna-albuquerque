namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>Seção <c>Fotos</c>. A mesma pasta é lida pelo Front.Web, que serve os arquivos em <c>/media</c>.</summary>
internal sealed class OpcoesDasFotos
{
    /// <summary>Caminho absoluto ou relativo à raiz do projeto. Na VPS é um volume, com backup próprio.</summary>
    public string Pasta { get; set; } = "";
}
