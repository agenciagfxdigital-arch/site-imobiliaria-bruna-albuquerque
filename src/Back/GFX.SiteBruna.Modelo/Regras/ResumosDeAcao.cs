using System.Collections;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>
/// Textos de confirmação que vão para o WhatsApp. São gerados pelo sistema (nunca por IA), para que a Bruna confirme
/// exatamente o que será executado.
/// </summary>
public static class ResumosDeAcao
{
    private static readonly HashSet<string> CamposDinheiro = ["preco", "aluguel", "condominio", "iptu", "entrada_a_partir"];

    public static string Alteracao(Imovel i, IEnumerable<MudancaCampo> mudancas, Func<Guid, string?> nomeDoBairro, IReadOnlyList<string> avisos) =>
        $"Confirma a alteração no {Cabecalho(i)}?\n{string.Join("\n", mudancas.Select(m => Linha(m, nomeDoBairro)))}{BlocoAvisos(avisos)}";

    public static string Desfazer(Imovel i, IEnumerable<MudancaCampo> mudancas, Func<Guid, string?> nomeDoBairro) =>
        $"Desfazer a última alteração no {Cabecalho(i)}?\n{string.Join("\n", mudancas.Select(m => Linha(m, nomeDoBairro)))}";

    public static string Publicacao(Imovel i, string? bairro, int totalDeFotos, IReadOnlyList<string> avisos)
    {
        var valor = i.Finalidade == Finalidade.Venda ? i.Preco : i.Aluguel;
        var linhaValor = valor is > 0 and var v
            ? $"{Formatos.Reais(v)}{(i.Finalidade == Finalidade.Aluguel ? "/mês" : "")} ({Extenso.Reais(v)})"
            : "sem valor informado";
        List<string> extras = [];
        if (totalDeFotos == 0) extras.Add("O imóvel está sem fotos.");
        if (valor is not > 0) extras.Add("O imóvel está sem valor; o site vai mostrar \"Consulte\".");

        var detalhes = $"• {Formatos.Rotulo(i.Finalidade)} · {Formatos.Rotulo(i.Tipo)}"
            + (bairro is null ? "" : $" · {bairro}")
            + (i.Quartos > 0 ? $" · {i.Quartos} quarto(s)" : "");
        return string.Join("\n", $"Publicar o {Cabecalho(i)} no site?", detalhes, $"• {linhaValor}", $"• {totalDeFotos} foto(s)")
            + BlocoAvisos([.. extras, .. avisos]);
    }

    public static string Status(Imovel i, StatusImovel novo) => novo switch
    {
        StatusImovel.Ativo => $"Reativar o {Cabecalho(i)} no site?",
        StatusImovel.Pausado => $"Pausar o {Cabecalho(i)}? Ele sai do site até ser reativado.",
        StatusImovel.Vendido => $"Marcar o {Cabecalho(i)} como *vendido*? Ele fica com o selo \"Vendido\" pelo período configurado.",
        StatusImovel.Alugado => $"Marcar o {Cabecalho(i)} como *alugado*? Ele fica com o selo \"Alugado\" pelo período configurado.",
        _ => $"Voltar o {Cabecalho(i)} para rascunho? Ele sai do site.",
    };

    public static string Destaque(Imovel i, bool destaque) => destaque
        ? $"Colocar o {Cabecalho(i)} nos destaques da página inicial?"
        : $"Tirar o {Cabecalho(i)} dos destaques da página inicial?";

    /// <summary>Valor em texto para o WhatsApp. <paramref name="nomeDoBairro"/> resolve o id do bairro para o nome.</summary>
    public static string FormatarValor(string campo, object? valor, Func<Guid, string?> nomeDoBairro) => valor switch
    {
        null or "" => "(vazio)",
        bool b => b ? "Sim" : "Não",
        string texto when campo == "descricao" => texto.Length > 120 ? $"{texto[..117]}…" : texto,
        string texto => texto,
        IEnumerable lista => lista.Cast<object>().Select(x => x.ToString()).ToArray() is { Length: > 0 } itens
            ? string.Join(", ", itens)
            : "(nenhum)",
        decimal d when CamposDinheiro.Contains(campo) => Formatos.Reais(d),
        decimal d when campo == "area" => Formatos.Area(d),
        Guid id when campo == "bairro_id" => nomeDoBairro(id) ?? "(bairro removido)",
        TipoImovel t => Formatos.Rotulo(t),
        Situacao s => Formatos.Rotulo(s),
        StatusImovel s => Formatos.Rotulo(s),
        _ => valor.ToString() ?? "",
    };

    private static string Linha(MudancaCampo m, Func<Guid, string?> nomeDoBairro)
    {
        var extenso = CamposDinheiro.Contains(m.Campo) && m.Depois is decimal d ? $" ({Extenso.Reais(d)})" : "";
        return $"• {Diferencas.RotuloDo(m.Campo)}: {FormatarValor(m.Campo, m.Antes, nomeDoBairro)} → *{FormatarValor(m.Campo, m.Depois, nomeDoBairro)}*{extenso}";
    }

    private static string Cabecalho(Imovel i) => $"*{i.Codigo} – {i.Titulo}*";

    private static string BlocoAvisos(IReadOnlyList<string> avisos) =>
        avisos.Count > 0 ? $"\n\n⚠️ {string.Join("\n⚠️ ", avisos)}" : "";
}
