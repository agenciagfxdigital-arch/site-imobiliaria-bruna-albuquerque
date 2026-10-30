namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Um campo que muda numa proposta (antes → depois). <c>Campo</c> usa os nomes das colunas (ex.: "preco").</summary>
public sealed record MudancaCampo(string Campo, object? Antes, object? Depois);
