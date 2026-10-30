using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Avisos de valores fora do comum. Não bloqueiam: aparecem no resumo para a Bruna conferir.</summary>
public static class Sanidade
{
    public static IReadOnlyList<string> Avisos(DadosDoImovel i)
    {
        List<string> avisos = [];
        if (i.Finalidade == Finalidade.Venda && i.Preco is { } preco && (preco < 30_000 || preco > 5_000_000))
            avisos.Add($"Confira o preço: {Formatos.Reais(preco)} está fora do comum para venda.");
        if (i.Finalidade == Finalidade.Aluguel && i.Aluguel is { } aluguel && (aluguel < 300 || aluguel > 30_000))
            avisos.Add($"Confira o aluguel: {Formatos.Reais(aluguel)} está fora do comum.");
        if (i.Area is > 2000 and var area) avisos.Add($"Confira a área: {Formatos.Area(area)} é muito grande.");
        if (i.Quartos > 10) avisos.Add($"Confira os quartos: {i.Quartos} é um número alto.");
        return avisos;
    }
}
