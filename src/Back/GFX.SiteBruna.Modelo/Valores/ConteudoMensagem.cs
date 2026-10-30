namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Conteúdo de uma mensagem recebida, já traduzido do formato da Meta para o do sistema.</summary>
public abstract record ConteudoMensagem
{
    private ConteudoMensagem() { }

    public sealed record Texto(string Conteudo) : ConteudoMensagem;

    public sealed record Imagem(string MidiaId, string? Mime, string? Legenda) : ConteudoMensagem;

    /// <summary>Toque num botão: o <c>Id</c> é o que o sistema mandou junto com o botão.</summary>
    public sealed record Botao(string Id, string? Titulo) : ConteudoMensagem;

    /// <summary>Áudio, documento, figurinha, localização…</summary>
    public sealed record Outro(string Descricao) : ConteudoMensagem;
}
