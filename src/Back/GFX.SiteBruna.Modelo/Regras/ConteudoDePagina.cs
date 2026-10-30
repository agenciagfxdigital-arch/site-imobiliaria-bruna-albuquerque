using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Limites dos textos editáveis das páginas (evitam colar algo gigante por engano e quebrar o layout).</summary>
public static class ConteudoDePagina
{
    public const int ItensPorLista = 30;
    public const int TamanhoDoItem = 300;
    public const int TamanhoDoTituloDoPasso = 200;
    public const int TamanhoDoTextoDoPasso = 1000;

    public static void Texto(string? valor, string campo, int maximo)
    {
        if (valor is null) throw DomainException.Validacao($"Preencha o campo {campo} (pode ficar vazio, mas não nulo).");
        if (valor.Length > maximo) throw DomainException.Validacao($"O campo {campo} pode ter até {maximo} letras.");
    }

    public static void Lista(IReadOnlyList<string>? itens, string campo)
    {
        if (itens is null) throw DomainException.Validacao($"Preencha o campo {campo}.");
        if (itens.Count > ItensPorLista) throw DomainException.Validacao($"Use até {ItensPorLista} itens em {campo}.");
        if (itens.Any(i => i is null || i.Length > TamanhoDoItem))
            throw DomainException.Validacao($"Cada item de {campo} pode ter até {TamanhoDoItem} letras.");
    }

    public static void Passos(IReadOnlyList<Passo>? passos, string campo)
    {
        if (passos is null) throw DomainException.Validacao($"Preencha o campo {campo}.");
        if (passos.Count > ItensPorLista) throw DomainException.Validacao($"Use até {ItensPorLista} passos em {campo}.");
        foreach (var p in passos)
        {
            if (string.IsNullOrWhiteSpace(p.Titulo)) throw DomainException.Validacao($"Cada passo de {campo} precisa de um título.");
            if (p.Titulo.Length > TamanhoDoTituloDoPasso || (p.Texto?.Length ?? 0) > TamanhoDoTextoDoPasso)
                throw DomainException.Validacao(
                    $"Em {campo}, o título do passo pode ter até {TamanhoDoTituloDoPasso} letras e o texto até {TamanhoDoTextoDoPasso}.");
        }
    }
}
