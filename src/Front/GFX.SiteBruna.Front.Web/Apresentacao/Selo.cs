using GFX.SiteBruna.Front.Web.DTOs;

namespace GFX.SiteBruna.Front.Web.Apresentacao;

/// <summary>Selo sobre a foto do imóvel ("Vendido", "Minha Casa Minha Vida", "Aceita pet"...).</summary>
public sealed record Selo(string Texto, TomDoSelo Tom)
{
    /// <summary>
    /// Mesma ordem e mesmas regras do site em Next. <paramref name="cartao"/>: versão enxuta da lista (sem "Aceita FGTS",
    /// que fica só na página do imóvel).
    /// </summary>
    public static IReadOnlyList<Selo> Do(
        StatusImovel status, Finalidade finalidade, bool mcmv, Situacao situacao, bool entradaParcelada, bool aceitaFgts,
        bool mobiliado, bool aceitaPet, bool cartao)
    {
        List<Selo> selos = [];
        if (status == StatusImovel.Vendido) selos.Add(new("Vendido", TomDoSelo.Vendido));
        if (status == StatusImovel.Alugado) selos.Add(new("Alugado", TomDoSelo.Vendido));
        if (finalidade == Finalidade.Venda)
        {
            if (mcmv) selos.Add(new("Minha Casa Minha Vida", TomDoSelo.Dourado));
            if (situacao == Situacao.NaPlanta) selos.Add(new("Na planta", TomDoSelo.Marinho));
            if (entradaParcelada) selos.Add(new("Entrada parcelada", TomDoSelo.Claro));
            if (aceitaFgts && !cartao) selos.Add(new("Aceita FGTS", TomDoSelo.Claro));
        }
        else
        {
            if (mobiliado) selos.Add(new("Mobiliado", TomDoSelo.Marinho));
            if (aceitaPet) selos.Add(new("Aceita pet", TomDoSelo.Claro));
        }
        return selos;
    }
}
