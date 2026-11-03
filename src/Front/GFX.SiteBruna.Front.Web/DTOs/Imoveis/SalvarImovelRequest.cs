namespace GFX.SiteBruna.Front.Web.DTOs.Imoveis;

/// <param name="Id">Vazio = imóvel novo.</param>
public sealed record SalvarImovelRequest(Guid? Id, DadosDoImovel Dados);
