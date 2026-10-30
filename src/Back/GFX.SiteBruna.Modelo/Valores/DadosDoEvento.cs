using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>O que o site envia ao registrar um evento. Sem dados pessoais: o visitante é um id aleatório do navegador.</summary>
public sealed record DadosDoEvento
{
    public TipoEvento Tipo { get; init; }
    public Guid? ImovelId { get; init; }
    public string? Pagina { get; init; }
    public string VisitanteId { get; init; } = "";
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
    public string? Referrer { get; init; }
    /// <summary>Detalhes curtos (ex.: filtro usado, rede do compartilhamento).</summary>
    public IReadOnlyDictionary<string, string?>? Detalhes { get; init; }
}
