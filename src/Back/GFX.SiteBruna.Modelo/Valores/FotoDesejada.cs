namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Foto na lista enviada pelo painel, já na ordem de exibição. Sem <c>Id</c> = foto nova.</summary>
public sealed record FotoDesejada(Guid? Id, string Url, string? CaminhoNoStorage, bool Capa);
