namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Foto na posição final, pronta para gravar. Sem <c>Id</c> = foto nova.</summary>
public sealed record FotoPlanejada(Guid? Id, string Url, string? CaminhoNoStorage, int Ordem, bool Capa);
