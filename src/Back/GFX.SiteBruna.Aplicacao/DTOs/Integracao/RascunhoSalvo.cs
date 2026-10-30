namespace GFX.SiteBruna.Aplicacao.DTOs.Integracao;

/// <param name="CamposAlterados">Na criação, zero.</param>
internal sealed record RascunhoSalvo(string Codigo, int CamposAlterados, IReadOnlyList<string> Avisos, string LinkPrevia);
