namespace GFX.SiteBruna.Front.Web.DTOs.Imoveis;

public sealed record FotoResponse(Guid Id, string Url, string? CaminhoNoStorage, int Ordem, bool Capa);
