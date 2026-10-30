using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.DTOs.Imoveis;

/// <summary>Imóvel para editar no painel: todos os campos (com os dados internos) e as fotos na ordem.</summary>
public sealed record ImovelDetalheResponse(
    Guid Id, string Slug, DadosDoImovel Dados, IReadOnlyList<FotoResponse> Fotos, OrigemImovel Origem,
    string? AtualizadoPor, DateTimeOffset? VendidoEm, DateTimeOffset CriadoEm, DateTimeOffset AtualizadoEm);
