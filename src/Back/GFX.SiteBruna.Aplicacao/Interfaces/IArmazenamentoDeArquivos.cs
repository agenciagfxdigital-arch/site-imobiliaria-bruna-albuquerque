namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Storage das imagens públicas (Supabase Storage).</summary>
public interface IArmazenamentoDeArquivos
{
    /// <summary>Grava o arquivo e devolve a URL pública.</summary>
    Task<string> Enviar(string caminho, byte[] conteudo, string tipo);

    Task Remover(IReadOnlyCollection<string> caminhos);
}
