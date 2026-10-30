using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

public sealed record FaqResponse(Guid Id, string Pergunta, string Resposta, int Ordem)
{
    public static FaqResponse De(Faq f) => new(f.Id, f.Pergunta, f.Resposta, f.Ordem);
}
