using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Aplicacao.DTOs.Imoveis;

/// <param name="Busca">Parte do código ou do título.</param>
public sealed record FiltroDeImoveis(
    string? Busca = null, StatusImovel? Status = null, Finalidade? Finalidade = null, Guid? BairroId = null, int Limite = 200);
