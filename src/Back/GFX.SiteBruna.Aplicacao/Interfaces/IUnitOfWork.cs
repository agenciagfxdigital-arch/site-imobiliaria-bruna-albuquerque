namespace GFX.SiteBruna.Aplicacao.Interfaces;

/// <summary>Grava de uma vez tudo o que os repositórios acompanharam. Nenhum repositório grava sozinho.</summary>
public interface IUnitOfWork
{
    /// <summary>Código de imóvel repetido vira <c>DomainException</c> <c>CodigoDuplicado</c>.</summary>
    Task Commit(CancellationToken ct = default);

    /// <summary>
    /// Esquece as alterações ainda não gravadas (ex.: uma confirmação falhou no meio). Entidades lidas antes deixam de
    /// ser acompanhadas: leia de novo o que for usar depois.
    /// </summary>
    void Descartar();
}
