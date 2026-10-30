using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Modelo.Regras;

public static partial class Telefone
{
    /// <summary>Normaliza para DDI + DDD + número, só dígitos (ex.: 5549999999999), com o nono dígito do celular.</summary>
    public static string Normalizar(string? valor)
    {
        var digitos = NaoDigito().Replace(valor ?? "", "");
        var comDdi = digitos.Length is 10 or 11 ? $"55{digitos}" : digitos;
        if (!DozeOuTrezeDigitos().IsMatch(comDdi))
            throw new DomainException(CodigoErro.NaoAutorizado, "Telefone do operador inválido.");
        // O WhatsApp às vezes manda celular brasileiro sem o nono dígito (55 + DDD + 8 dígitos começando em 6–9).
        var semNono = CelularSemNono().Match(comDdi);
        return semNono.Success ? $"55{semNono.Groups[1].Value}9{semNono.Groups[2].Value}" : comDdi;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigito();

    [GeneratedRegex("^[0-9]{12,13}$")]
    private static partial Regex DozeOuTrezeDigitos();

    [GeneratedRegex(@"^55(\d{2})([6-9]\d{7})$")]
    private static partial Regex CelularSemNono();
}
