namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

/// <param name="Id">Vazio = bairro novo.</param>
public sealed record SalvarBairroRequest(Guid? Id, string Nome);
