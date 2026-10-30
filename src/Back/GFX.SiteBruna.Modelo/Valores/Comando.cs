namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>O que uma mensagem recebida significa para o assistente.</summary>
public abstract record Comando
{
    private Comando() { }

    public sealed record FichaEnviada(string Texto) : Comando;

    public sealed record PedirFicha : Comando;

    public sealed record Selecionar(string Codigo) : Comando;

    /// <summary>"publicar" sozinho usa o imóvel atual da conversa.</summary>
    public sealed record Publicar(string? Codigo) : Comando;

    public sealed record Confirmar(Guid AcaoId) : Comando;

    public sealed record Cancelar(Guid AcaoId) : Comando;

    /// <summary>"sim" por texto: vale só quando há exatamente uma confirmação aberta.</summary>
    public sealed record Sim : Comando;

    public sealed record Nao : Comando;

    /// <summary>Foto; com o código na legenda, vai para aquele imóvel.</summary>
    public sealed record Foto(string MidiaId, string? Codigo) : Comando;

    public sealed record NaoSuportado(string Descricao) : Comando;

    public sealed record Ajuda : Comando;
}
