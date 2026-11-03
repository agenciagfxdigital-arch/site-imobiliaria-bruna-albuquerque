using GFX.SiteBruna.Front.Web.DTOs;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>Nomes que aparecem na tela para os valores dos enums.</summary>
public static class Rotulos
{
    public static readonly IReadOnlyList<TipoImovel> Tipos = Enum.GetValues<TipoImovel>();

    public static string Tipo(TipoImovel tipo) => tipo switch
    {
        TipoImovel.Apartamento => "Apartamento",
        TipoImovel.Casa => "Casa",
        TipoImovel.Terreno => "Terreno",
        _ => "Sala comercial",
    };

    public static string TipoNoPlural(TipoImovel tipo) => tipo switch
    {
        TipoImovel.Apartamento => "Apartamentos",
        TipoImovel.Casa => "Casas",
        TipoImovel.Terreno => "Terrenos",
        _ => "Salas comerciais",
    };

    public static string Finalidade(Finalidade finalidade) => finalidade == DTOs.Finalidade.Venda ? "Venda" : "Aluguel";

    public static string Status(StatusImovel status) => status switch
    {
        StatusImovel.Rascunho => "Rascunho",
        StatusImovel.Ativo => "Ativo",
        StatusImovel.Pausado => "Pausado",
        StatusImovel.Vendido => "Vendido",
        _ => "Alugado",
    };

    public static string Situacao(Situacao situacao) => situacao == DTOs.Situacao.NaPlanta ? "Na planta" : "Pronto";

    /// <summary>Diferenciais sugeridos no painel (a Bruna pode escrever outros).</summary>
    public static readonly IReadOnlyList<string> Diferenciais =
    [
        "Sacada", "Sacada com churrasqueira", "Churrasqueira", "Piscina", "Elevador", "Portaria", "Salão de festas",
        "Playground", "Academia", "Área de serviço", "Quintal", "Pátio fechado", "Ar-condicionado", "Móveis planejados",
        "Energia solar", "Aquecimento a gás", "Hidromassagem", "Lavanderia", "Depósito", "Vista panorâmica",
    ];

    public static readonly IReadOnlyList<string> Garantias = ["Caução", "Fiador", "Seguro-fiança", "Título de capitalização"];
}
