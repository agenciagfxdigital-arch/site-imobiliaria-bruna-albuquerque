namespace GFX.SiteBruna.Aplicacao.DTOs.Imagens;

/// <param name="Aviso">Para mostrar a quem enviou (ex.: foto pequena). Não impede o envio.</param>
public sealed record ImagemEnviadaResponse(string Url, string CaminhoNoStorage, string? Aviso);
