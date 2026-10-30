using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Imoveis;

/// <summary>Só para o painel e o assistente; nunca para o site.</summary>
public sealed record DadosInternosResponse(string? ProprietarioNome, string Endereco, string? AtualizadoPor)
{
    public static DadosInternosResponse De(DadosInternosImovel? d) =>
        d is null ? new(null, "", null) : new(d.ProprietarioNome, d.Endereco, d.AtualizadoPor);
}
