using Microsoft.AspNetCore.Components.Forms;

namespace GFX.SiteBruna.Front.Web.Components.Painel;

/// <summary>Leitura das fotos escolhidas no painel (o limite é o mesmo da Api: 15 MB).</summary>
internal static class Arquivos
{
    public const long TamanhoMaximo = 15 * 1024 * 1024;
    public const string MensagemGrandeDemais = "A foto é grande demais (máximo de 15 MB).";

    /// <exception cref="IOException">Arquivo maior que o limite.</exception>
    public static async Task<byte[]> Ler(IBrowserFile arquivo)
    {
        await using var fluxo = arquivo.OpenReadStream(TamanhoMaximo);
        using var memoria = new MemoryStream();
        await fluxo.CopyToAsync(memoria);
        return memoria.ToArray();
    }
}
