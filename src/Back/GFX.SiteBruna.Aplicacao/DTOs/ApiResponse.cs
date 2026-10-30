using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Aplicacao.DTOs;

/// <summary>Envelope de todas as respostas dos casos de uso.</summary>
public sealed record ApiResponse<T>
{
    public bool Sucesso { get; init; }
    public T? Dados { get; init; }
    public TipoFalha? Falha { get; init; }
    public IReadOnlyList<string> Mensagens { get; init; } = [];
    /// <summary>Alternativas para quem chamou (ex.: códigos de imóveis parecidos).</summary>
    public IReadOnlyList<string> Sugestoes { get; init; } = [];

    public static ApiResponse<T> Ok(T dados) => new() { Sucesso = true, Dados = dados };

    public static ApiResponse<T> Erro(TipoFalha falha, string mensagem) =>
        new() { Sucesso = false, Falha = falha, Mensagens = [mensagem] };

    /// <summary>Converte o erro do domínio, mantendo a mensagem já escrita para o usuário.</summary>
    public static ApiResponse<T> De(DomainException erro) => new()
    {
        Sucesso = false,
        Falha = erro.Codigo switch
        {
            CodigoErro.NaoEncontrado => TipoFalha.NaoEncontrado,
            CodigoErro.CodigoDuplicado or CodigoErro.Conflito or CodigoErro.Expirada => TipoFalha.Conflito,
            CodigoErro.NaoAutorizado => TipoFalha.NaoAutorizado,
            CodigoErro.Limite => TipoFalha.Limite,
            _ => TipoFalha.Validacao,
        },
        Mensagens = [erro.Message],
        Sugestoes = erro.Sugestoes,
    };
}
