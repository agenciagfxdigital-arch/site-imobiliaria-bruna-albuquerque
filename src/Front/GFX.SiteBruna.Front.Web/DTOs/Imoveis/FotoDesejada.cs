namespace GFX.SiteBruna.Front.Web.DTOs.Imoveis;

/// <summary>Foto na lista enviada ao salvar, já na ordem de exibição. Sem <c>Id</c> = foto nova (já enviada ao storage).</summary>
public sealed record FotoDesejada(Guid? Id, string Url, string? CaminhoNoStorage, bool Capa);
