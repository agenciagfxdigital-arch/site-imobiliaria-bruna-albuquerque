namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

public sealed record FaqResponse(Guid Id, string Pergunta, string Resposta, int Ordem);
