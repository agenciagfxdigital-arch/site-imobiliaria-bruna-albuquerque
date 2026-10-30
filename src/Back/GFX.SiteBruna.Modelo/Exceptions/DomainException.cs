namespace GFX.SiteBruna.Modelo.Exceptions;

/// <summary>
/// Erro de regra de negócio. A mensagem já é escrita para quem usa o painel ou o WhatsApp (pt-BR).
/// </summary>
public class DomainException(CodigoErro codigo, string mensagem, IReadOnlyList<string>? sugestoes = null)
    : Exception(mensagem)
{
    public CodigoErro Codigo { get; } = codigo;

    /// <summary>Alternativas para quem chamou (ex.: códigos de imóveis parecidos).</summary>
    public IReadOnlyList<string> Sugestoes { get; } = sugestoes ?? [];

    public static DomainException Validacao(string mensagem) => new(CodigoErro.Validacao, mensagem);
}
