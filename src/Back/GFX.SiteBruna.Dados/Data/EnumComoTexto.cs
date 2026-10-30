using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GFX.SiteBruna.Dados.Data;

/// <summary>Grava o enum como texto em snake_case ("SalaComercial" → "sala_comercial"), como no banco do site em Next.</summary>
internal sealed class EnumComoTexto<T>() : ValueConverter<T, string>(v => ParaTexto(v), v => DeTexto(v))
    where T : struct, Enum
{
    private static readonly Dictionary<T, string> Textos = Enum.GetValues<T>().ToDictionary(v => v, v => SnakeCase(v.ToString()));
    private static readonly Dictionary<string, T> Valores = Textos.ToDictionary(x => x.Value, x => x.Key);

    public static string ParaTexto(T valor) => Textos[valor];

    public static T DeTexto(string texto) => Valores[texto];

    /// <summary>Valores aceitos, para o <c>check</c> da coluna.</summary>
    public static string ListaSql => string.Join(", ", Textos.Values.Select(t => $"'{t}'"));

    private static string SnakeCase(string nome)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < nome.Length; i++)
        {
            if (i > 0 && char.IsUpper(nome[i])) sb.Append('_');
            sb.Append(char.ToLowerInvariant(nome[i]));
        }
        return sb.ToString();
    }
}
