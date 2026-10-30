using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

public sealed record DepoimentoResponse(Guid Id, string Nome, string Texto, string? FotoUrl, bool ExibirNaHome, int Ordem)
{
    public static DepoimentoResponse De(Depoimento d) => new(d.Id, d.Nome, d.Texto, d.FotoUrl, d.ExibirNaHome, d.Ordem);
}
