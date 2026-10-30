using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Pergunta frequente (página do Minha Casa Minha Vida).</summary>
public sealed class Faq
{
    public Guid Id { get; private set; }
    public string Pergunta { get; private set; } = "";
    public string Resposta { get; private set; } = "";
    public int Ordem { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private Faq() { } // EF

    public Faq(string pergunta, string resposta, int ordem, DateTimeOffset agora)
    {
        Id = Guid.CreateVersion7();
        CriadoEm = agora;
        Atualizar(pergunta, resposta, ordem);
    }

    public void Atualizar(string pergunta, string resposta, int ordem)
    {
        var p = (pergunta ?? "").Trim();
        var r = (resposta ?? "").Trim();
        if (p.Length < 3) throw DomainException.Validacao("Escreva a pergunta.");
        if (p.Length > 300) throw DomainException.Validacao("A pergunta pode ter até 300 letras.");
        if (r.Length < 3) throw DomainException.Validacao("Escreva a resposta.");
        if (r.Length > 3000) throw DomainException.Validacao("A resposta pode ter até 3000 letras.");
        Pergunta = p;
        Resposta = r;
        DefinirOrdem(ordem);
    }

    public void DefinirOrdem(int ordem)
    {
        if (ordem is < 0 or > 999) throw DomainException.Validacao("A ordem deve ficar entre 0 e 999.");
        Ordem = ordem;
    }

    /// <summary>
    /// Nova ordem a partir da lista arrastada no painel (1, 2, 3…). Precisa trazer todas as perguntas, uma vez cada.
    /// </summary>
    public static void Reordenar(IReadOnlyList<Faq> todas, IReadOnlyList<Guid> novaOrdem)
    {
        if (novaOrdem.Count != todas.Count || novaOrdem.Distinct().Count() != novaOrdem.Count
            || !novaOrdem.All(id => todas.Any(f => f.Id == id)))
            throw DomainException.Validacao("A lista mudou enquanto você organizava. Recarregue a página e tente de novo.");
        var porId = todas.ToDictionary(f => f.Id);
        for (var i = 0; i < novaOrdem.Count; i++) porId[novaOrdem[i]].DefinirOrdem(i + 1);
    }
}
