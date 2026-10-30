using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

public sealed record BairroResponse(Guid Id, string Nome, string Slug)
{
    public static BairroResponse De(Bairro b) => new(b.Id, b.Nome, b.Slug);
}
