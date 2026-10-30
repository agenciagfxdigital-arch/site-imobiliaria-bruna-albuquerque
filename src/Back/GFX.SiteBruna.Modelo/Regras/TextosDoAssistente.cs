using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Textos que o assistente envia (formatação do WhatsApp: *negrito*, • listas).</summary>
public static class TextosDoAssistente
{
    public const string Ajuda = """
        Oi! Por aqui você cadastra imóveis no site 🏡

        • Mande *ficha* para receber o modelo de cadastro.
        • Preencha e envie a ficha: eu crio o rascunho.
        • Depois envie as fotos (a primeira vira a capa).
        • Mande o *código* (ex.: BA107) para escolher o imóvel que vai receber fotos.
        • Mande *publicar BA107* para colocar no site.

        Status, destaques e exclusões continuam no painel.
        """;

    public const string InstrucaoFicha =
        "Copie a ficha abaixo, preencha e me envie. Campos que não se aplicam podem ficar em branco. Para alterar um imóvel, acrescente a linha *Código: BA107*.";

    public const string NaoSuportado =
        "Por enquanto eu entendo texto e fotos. Para cadastrar, mande *ficha*; para ver as opções, mande *ajuda*.";

    public const string SemImovelParaFotos =
        "De qual imóvel são as fotos? Mande primeiro a ficha preenchida ou o código do imóvel (ex.: BA107) e depois as fotos.";

    public const string NadaPendente = "Não há nada esperando confirmação agora.";
    public const string VariasPendentes = "Há mais de uma confirmação aberta. Toque no botão da que você quer.";
    public const string ErroInesperado = "Não consegui concluir agora. Tente de novo em alguns instantes.";
    public const string Limite = "Recebi muitas mensagens em pouco tempo. Espere alguns minutos e tente de novo.";

    public static string FotoDeImovelPublicado(string codigo) =>
        $"O {codigo} já está no site. Por segurança, fotos de imóveis publicados são trocadas pelo painel.";

    public static string PublicacaoAdiada(string codigo) =>
        $"Tudo bem, o {codigo} continua como rascunho. Quando quiser, mande *publicar {codigo}*.";

    public static string ProblemasDaFicha(IEnumerable<string> problemas) =>
        string.Join("\n", ["Preciso de alguns ajustes na ficha:", .. problemas.Select(p => $"• {p}"), "", "Corrija e envie a ficha de novo."]);

    public static string RascunhoCriado(string codigo, IEnumerable<string> avisos, string linkPrevia) =>
        string.Join("\n",
        [
            $"Rascunho *{codigo}* criado ✅ (ainda não aparece no site).",
            .. avisos.Select(a => $"⚠️ {a}"),
            "",
            "Agora envie as fotos. A primeira vira a capa.",
            $"Prévia: {linkPrevia}",
        ]);

    public static string RascunhoAtualizado(string codigo, int camposAlterados, IEnumerable<string> avisos) =>
        string.Join("\n",
        [
            $"Rascunho *{codigo}* atualizado ✅",
            camposAlterados > 0 ? $"Campos atualizados: {camposAlterados}." : "Nada mudou: os valores já estavam salvos.",
            .. avisos.Select(a => $"⚠️ {a}"),
        ]);

    public static string ImovelSelecionado(string codigo, string titulo, StatusImovel status) =>
        status == StatusImovel.Rascunho
            ? $"Ok! As próximas fotos vão para o *{codigo} – {titulo}*."
            : $"O *{codigo} – {titulo}* já está no site. Fotos de imóveis publicados são trocadas pelo painel.";

    public static string FotosRecebidas(string resumo) => $"Fotos recebidas ✅\n\n{resumo}";

    public static string ComLink(string mensagem, string? link) => link is null ? mensagem : $"{mensagem}\n{link}";
}
