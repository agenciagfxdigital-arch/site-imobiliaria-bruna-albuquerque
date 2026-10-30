using System.Globalization;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>
/// Valores em reais por extenso, para a Bruna conferir o número no resumo de confirmação
/// ("R$ 349.000 (trezentos e quarenta e nove mil reais)"). Centavos são arredondados.
/// </summary>
public static class Extenso
{
    private static readonly string[] Unidades = ["", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove"];
    private static readonly string[] DezADezenove =
        ["dez", "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove"];
    private static readonly string[] Dezenas = ["", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"];
    private static readonly string[] Centenas =
        ["", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"];

    public static string Numero(decimal valor)
    {
        var n = Arredondar(valor);
        if (n == 0) return "zero";
        if (n >= 1_000_000_000) return n.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"));

        var milhoes = n / 1_000_000;
        var milhares = n % 1_000_000 / 1000;
        var unidades = n % 1000;

        var texto = "";
        if (milhoes > 0) texto = milhoes == 1 ? "um milhão" : $"{Ate999(milhoes)} milhões";
        if (milhares > 0)
        {
            var parte = milhares == 1 ? "mil" : $"{Ate999(milhares)} mil";
            texto = texto.Length > 0 ? $"{texto}{Conector(milhares, unidades)}{parte}" : parte;
        }
        if (unidades > 0) texto = texto.Length > 0 ? $"{texto}{Conector(unidades, 0)}{Ate999(unidades)}" : Ate999(unidades);
        return texto;
    }

    public static string Reais(decimal valor)
    {
        var n = Arredondar(valor);
        var redondoEmMilhao = n >= 1_000_000 && n % 1_000_000 == 0;
        return $"{Numero(n)}{(redondoEmMilhao ? " de" : "")} {(n == 1 ? "real" : "reais")}";
    }

    private static long Arredondar(decimal valor) => (long)Math.Round(Math.Abs(valor), MidpointRounding.AwayFromZero);

    /// <summary>1 a 999.</summary>
    private static string Ate999(long n)
    {
        if (n == 100) return "cem";
        var c = n / 100;
        var resto = n % 100;
        List<string> partes = [];
        if (c > 0) partes.Add(Centenas[c]);
        if (resto is >= 10 and < 20) partes.Add(DezADezenove[resto - 10]);
        else
        {
            if (resto / 10 > 0) partes.Add(Dezenas[resto / 10]);
            if (resto % 10 > 0) partes.Add(Unidades[resto % 10]);
        }
        return string.Join(" e ", partes);
    }

    /// <summary>
    /// O "e" entre grupos aparece quando o grupo seguinte é o último com valor e é menor que 100 ou uma centena redonda:
    /// "mil e quinhentos", "um milhão e duzentos mil", mas "um milhão duzentos e cinquenta mil".
    /// </summary>
    private static string Conector(long grupoSeguinte, long restoDepois) =>
        restoDepois == 0 && (grupoSeguinte < 100 || grupoSeguinte % 100 == 0) ? " e " : " ";
}
