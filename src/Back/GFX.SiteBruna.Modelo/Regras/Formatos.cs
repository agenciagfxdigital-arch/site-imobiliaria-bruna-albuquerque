using System.Globalization;
using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Valores em texto, como aparecem para a Bruna (WhatsApp) e no site.</summary>
public static class Formatos
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>"R$ 365.000" (sem centavos).</summary>
    public static string Reais(decimal valor) =>
        $"R$ {Math.Round(valor, 0, MidpointRounding.AwayFromZero).ToString("N0", PtBr)}";

    /// <summary>"74,5 m²".</summary>
    public static string Area(decimal valor) => $"{valor.ToString("#,##0.##", PtBr)} m²";

    public static string Rotulo(Finalidade finalidade) => finalidade == Finalidade.Venda ? "Venda" : "Aluguel";

    public static string Rotulo(TipoImovel tipo) => tipo switch
    {
        TipoImovel.Apartamento => "Apartamento",
        TipoImovel.Casa => "Casa",
        TipoImovel.Terreno => "Terreno",
        _ => "Sala comercial",
    };

    public static string Rotulo(Situacao situacao) => situacao == Situacao.NaPlanta ? "Na planta" : "Pronto";

    public static string Rotulo(StatusImovel status) => status switch
    {
        StatusImovel.Rascunho => "Rascunho",
        StatusImovel.Ativo => "Ativo",
        StatusImovel.Pausado => "Pausado",
        StatusImovel.Vendido => "Vendido",
        _ => "Alugado",
    };
}
