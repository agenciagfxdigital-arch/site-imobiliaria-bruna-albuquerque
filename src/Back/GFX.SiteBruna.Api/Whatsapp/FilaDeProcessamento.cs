using System.Threading.Channels;

namespace GFX.SiteBruna.Api.Whatsapp;

/// <summary>
/// Telefones com mensagens novas na fila do banco. O webhook avisa aqui e responde 200 na hora; o
/// <see cref="ProcessadorDoWhatsapp"/> processa em segundo plano (a espera de 15 s das fotos não segura a Meta).
/// </summary>
public sealed class FilaDeProcessamento
{
    private readonly Channel<string> _canal = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

    public void Avisar(string telefone) => _canal.Writer.TryWrite(telefone);

    public IAsyncEnumerable<string> Ler(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
}
