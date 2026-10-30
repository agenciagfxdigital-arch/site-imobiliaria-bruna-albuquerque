using System.Security.Cryptography;
using System.Text;

namespace GFX.SiteBruna.Api.Whatsapp;

/// <summary>Confere que a chamada veio da Meta: X-Hub-Signature-256 = HMAC-SHA256 do corpo bruto com o App Secret.</summary>
public static class AssinaturaDoWebhook
{
    private const string Prefixo = "sha256=";

    public static bool Valida(ReadOnlySpan<byte> corpo, string? cabecalho, string appSecret)
    {
        if (string.IsNullOrEmpty(appSecret) || cabecalho is null || !cabecalho.StartsWith(Prefixo, StringComparison.Ordinal))
            return false;
        byte[] recebida;
        try
        {
            recebida = Convert.FromHexString(cabecalho[Prefixo.Length..]);
        }
        catch (FormatException)
        {
            return false;
        }
        var esperada = HMACSHA256.HashData(Encoding.UTF8.GetBytes(appSecret), corpo);
        return CryptographicOperations.FixedTimeEquals(recebida, esperada);
    }

    /// <summary>Comparação em tempo constante (token de verificação).</summary>
    public static bool Iguais(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
