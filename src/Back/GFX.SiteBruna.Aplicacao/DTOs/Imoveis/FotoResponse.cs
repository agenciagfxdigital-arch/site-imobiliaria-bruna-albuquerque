using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Imoveis;

public sealed record FotoResponse(Guid Id, string Url, string? CaminhoNoStorage, int Ordem, bool Capa)
{
    public static FotoResponse De(FotoImovel f) => new(f.Id, f.Url, f.CaminhoNoStorage, f.Ordem, f.Capa);
}
