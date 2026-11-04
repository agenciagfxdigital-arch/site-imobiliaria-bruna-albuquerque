using GFX.SiteBruna.Aplicacao.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>
/// Imagens públicas em disco, na pasta <c>Fotos:Pasta</c>. A URL gravada é relativa (<c>/media/...</c>): trocar de domínio
/// não exige mexer no banco.
/// </summary>
internal sealed class ArmazenamentoEmDisco(
    IOptions<OpcoesDasFotos> opcoes, IHostEnvironment ambiente, ILogger<ArmazenamentoEmDisco> logger)
    : IArmazenamentoDeArquivos
{
    private const string PrefixoDaUrl = "/media";

    public async Task<string> Enviar(string caminho, byte[] conteudo, string tipo)
    {
        var destino = CaminhoCompleto(caminho);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);

        // Grava ao lado e só depois move: o site nunca serve um arquivo pela metade
        var temporario = $"{destino}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temporario, conteudo);
            File.Move(temporario, destino, overwrite: false);
        }
        finally
        {
            File.Delete(temporario);
        }
        return $"{PrefixoDaUrl}/{caminho}";
    }

    public Task Remover(IReadOnlyCollection<string> caminhos)
    {
        foreach (var caminho in caminhos)
        {
            // Arquivo que sobrou no disco não quebra o site: registra e segue.
            try
            {
                File.Delete(CaminhoCompleto(caminho));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                logger.LogWarning(e, "[fotos] não foi possível remover {Caminho}", caminho);
            }
        }
        return Task.CompletedTask;
    }

    private string CaminhoCompleto(string caminho)
    {
        if (string.IsNullOrWhiteSpace(opcoes.Value.Pasta))
            throw new InvalidOperationException("Configure Fotos:Pasta para gravar imagens.");

        var raiz = Path.TrimEndingDirectorySeparator(Path.GetFullPath(opcoes.Value.Pasta, ambiente.ContentRootPath));
        var completo = Path.GetFullPath(caminho, raiz);
        if (!completo.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException($"Caminho fora da pasta de fotos: {caminho}");
        return completo;
    }
}
