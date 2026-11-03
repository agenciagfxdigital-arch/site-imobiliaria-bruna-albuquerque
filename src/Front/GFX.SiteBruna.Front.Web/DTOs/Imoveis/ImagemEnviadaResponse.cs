namespace GFX.SiteBruna.Front.Web.DTOs.Imoveis;

/// <param name="Aviso">Para mostrar a quem enviou (ex.: foto pequena). Não impede o envio.</param>
public sealed record ImagemEnviadaResponse(string Url, string CaminhoNoStorage, string? Aviso);
