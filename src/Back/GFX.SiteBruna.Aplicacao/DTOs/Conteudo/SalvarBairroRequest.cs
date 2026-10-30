namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

/// <param name="Id">Vazio = bairro novo.</param>
public sealed record SalvarBairroRequest(Guid? Id, string Nome);
