using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.DTOs.Imoveis;

/// <param name="Id">Vazio = imóvel novo.</param>
public sealed record SalvarImovelRequest(Guid? Id, DadosDoImovel Dados);
