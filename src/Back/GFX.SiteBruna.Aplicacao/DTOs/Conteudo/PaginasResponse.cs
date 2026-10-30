using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

public sealed record PaginasResponse(ConteudoHome Home, ConteudoSobre Sobre, ConteudoMcmv Mcmv)
{
    public static PaginasResponse De(Paginas p) => new(p.Home, p.Sobre, p.Mcmv);
}
