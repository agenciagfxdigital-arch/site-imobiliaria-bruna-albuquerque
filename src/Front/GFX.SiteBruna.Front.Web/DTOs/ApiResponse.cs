namespace GFX.SiteBruna.Front.Web.DTOs;

/// <summary>
/// Espelho do envelope da Api (<c>GFX.SiteBruna.Aplicacao.DTOs.ApiResponse&lt;T&gt;</c>). O front não referencia o back:
/// os contratos são copiados (docs/ARQUITETURA.md, regra 4).
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Sucesso { get; set; }
    public T? Dados { get; set; }
    /// <summary>"Validacao", "NaoEncontrado", "Conflito", "NaoAutorizado" ou "Limite" (a Api manda o enum como texto).</summary>
    public string? Falha { get; set; }
    public List<string> Mensagens { get; set; } = [];
    public List<string> Sugestoes { get; set; } = [];

    public bool NaoEncontrado => Falha == "NaoEncontrado";

    /// <summary>Falhas que nascem no próprio front (sem conexão, resposta fora do envelope).</summary>
    public static ApiResponse<T> Erro(string mensagem) => new() { Sucesso = false, Mensagens = [mensagem] };
}
