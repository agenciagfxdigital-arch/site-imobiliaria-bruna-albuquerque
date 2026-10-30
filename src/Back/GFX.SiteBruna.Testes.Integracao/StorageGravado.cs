using System.Collections.Concurrent;
using GFX.SiteBruna.Aplicacao.Interfaces;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>Storage de mentira: guarda os arquivos em memória.</summary>
public sealed class StorageGravado : IArmazenamentoDeArquivos
{
    public ConcurrentDictionary<string, byte[]> Arquivos { get; } = new();

    public Task<string> Enviar(string caminho, byte[] conteudo, string tipo)
    {
        Arquivos[caminho] = conteudo;
        return Task.FromResult($"https://storage.teste/{caminho}");
    }

    public Task Remover(IReadOnlyCollection<string> caminhos)
    {
        foreach (var c in caminhos) Arquivos.TryRemove(c, out _);
        return Task.CompletedTask;
    }
}
