namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

/// <param name="Id">Vazio = pergunta nova.</param>
public sealed record SalvarFaqRequest(Guid? Id, string Pergunta, string Resposta, int Ordem);
