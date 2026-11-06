namespace GFX.SiteBruna.Dados.Repositories;

/// <summary>Resultado das consultas em SQL do <see cref="MetricasRepository"/>.</summary>
internal sealed class LinhaDeTotal<T>
{
    public required T Chave { get; init; }
    public required int Total { get; init; }
}
