namespace GFX.SiteBruna.Modelo.Regras;

/// <summary>Listas oficiais do imóvel (os nomes que aparecem nos filtros e nas fichas).</summary>
public static class Opcoes
{
    public static readonly IReadOnlyList<string> Diferenciais =
    [
        "Sacada", "Sacada com churrasqueira", "Churrasqueira", "Piscina", "Elevador", "Portaria", "Salão de festas",
        "Playground", "Academia", "Área de serviço", "Quintal", "Pátio fechado", "Ar-condicionado", "Móveis planejados",
        "Energia solar", "Aquecimento a gás", "Hidromassagem", "Lavanderia", "Depósito", "Vista panorâmica",
    ];

    public static readonly IReadOnlyList<string> Garantias = ["Caução", "Fiador", "Seguro-fiança", "Título de capitalização"];

    /// <summary>Troca o que foi escrito pelo nome oficial quando bate (sem acento/maiúscula); o resto continua livre.</summary>
    public static IReadOnlyList<string> Canonicos(IEnumerable<string> valores, IReadOnlyList<string> oficiais) =>
        [.. valores.Select(v => oficiais.FirstOrDefault(o => Texto.Normalizar(o) == Texto.Normalizar(v)) ?? v)];
}
