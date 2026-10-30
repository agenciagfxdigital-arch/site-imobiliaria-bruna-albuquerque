namespace GFX.SiteBruna.Aplicacao.DTOs.Integracao;

/// <param name="Resumo">Texto gerado pelo sistema, mostrado junto com os botões.</param>
internal sealed record PropostaFeita(Guid AcaoId, string Resumo, IReadOnlyList<string> Avisos);
