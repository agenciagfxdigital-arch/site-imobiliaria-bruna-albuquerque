using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>O assistente do painel pelo WhatsApp (fase 1, sem IA): o que cada mensagem significa e os botões.</summary>
public static partial class Assistente
{
    /// <summary>Fotos chegam uma por mensagem: a resposta sai quando fica este tempo sem chegar mensagem nova.</summary>
    public static readonly TimeSpan EsperaFotos = TimeSpan.FromSeconds(15);

    private static readonly string[] PedidosDeFicha = ["ficha", "modelo", "cadastrar", "cadastro", "novo imovel", "novo"];
    private static readonly string[] Confirmacoes = ["sim", "s", "pode", "confirmo", "confirma", "ok", "pode publicar"];
    private static readonly string[] Negativas = ["nao", "n", "cancela", "cancelar", "agora nao"];

    public static IReadOnlyList<BotaoDoWhatsapp> BotoesDePublicacao(Guid acaoId) =>
        [new($"ok:{acaoId:D}", "Publicar"), new($"no:{acaoId:D}", "Agora não")];

    public static IReadOnlyList<BotaoDoWhatsapp> BotoesDeConfirmacao(Guid acaoId) =>
        [new($"ok:{acaoId:D}", "Confirmar"), new($"no:{acaoId:D}", "Cancelar")];

    public static Comando Interpretar(ConteudoMensagem conteudo) => conteudo switch
    {
        ConteudoMensagem.Botao b => InterpretarBotao(b.Id),
        ConteudoMensagem.Imagem i => new Comando.Foto(i.MidiaId, CodigoNaLegenda(i.Legenda)),
        ConteudoMensagem.Outro o => new Comando.NaoSuportado(o.Descricao),
        ConteudoMensagem.Texto t => InterpretarTexto(t.Conteudo),
        _ => new Comando.Ajuda(),
    };

    /// <summary>Só aceita os ids gerados pelo sistema ("ok:" ou "no:" + id da ação).</summary>
    private static Comando InterpretarBotao(string id)
    {
        var partes = id.Split(':');
        if (partes.Length == 2 && Guid.TryParseExact(partes[1], "D", out var acaoId))
        {
            if (partes[0] == "ok") return new Comando.Confirmar(acaoId);
            if (partes[0] == "no") return new Comando.Cancelar(acaoId);
        }
        return new Comando.Ajuda();
    }

    private static Comando InterpretarTexto(string texto)
    {
        if (Ficha.EhFicha(texto)) return new Comando.FichaEnviada(texto);
        var t = PontuacaoFinal().Replace(Texto.Normalizar(texto), "");
        if (CodigoDoImovel().IsMatch(t)) return new Comando.Selecionar(t.ToUpperInvariant());
        var publicar = PedidoDePublicacao().Match(t);
        if (publicar.Success)
            return new Comando.Publicar(publicar.Groups[1].Success ? publicar.Groups[1].Value.ToUpperInvariant() : null);
        if (PedidosDeFicha.Contains(t)) return new Comando.PedirFicha();
        if (Confirmacoes.Contains(t)) return new Comando.Sim();
        if (Negativas.Contains(t)) return new Comando.Nao();
        return new Comando.Ajuda();
    }

    private static string? CodigoNaLegenda(string? legenda)
    {
        var t = legenda?.Trim() ?? "";
        return CodigoDoImovel().IsMatch(t) ? t.ToUpperInvariant() : null;
    }

    [GeneratedRegex("^[A-Za-z]{1,6}[0-9]{1,10}$")]
    private static partial Regex CodigoDoImovel();

    [GeneratedRegex("^publicar(?:\\s+([a-z]{1,6}[0-9]{1,10}))?$")]
    private static partial Regex PedidoDePublicacao();

    [GeneratedRegex("[!.?]+$")]
    private static partial Regex PontuacaoFinal();
}
