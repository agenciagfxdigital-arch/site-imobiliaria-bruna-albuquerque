using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Evento de navegação para as métricas do painel. Sem dados pessoais nem IP.</summary>
public sealed partial class Evento
{
    public Guid Id { get; private set; }
    public TipoEvento Tipo { get; private set; }
    public Guid? ImovelId { get; private set; }
    public string? Pagina { get; private set; }
    public string VisitanteId { get; private set; } = "";
    public string? UtmSource { get; private set; }
    public string? UtmMedium { get; private set; }
    public string? UtmCampaign { get; private set; }
    public string? Referrer { get; private set; }
    public Dispositivo Dispositivo { get; private set; }
    public Dictionary<string, string?>? Detalhes { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private Evento() { } // EF

    /// <summary>Monta o evento; dados fora do formato são recusados (o site não mostra erro ao visitante).</summary>
    public Evento(DadosDoEvento d, Dispositivo dispositivo, DateTimeOffset agora)
    {
        if (!VisitanteValido().IsMatch(d.VisitanteId ?? "")) throw DomainException.Validacao("Visitante inválido.");
        if (d.Tipo == TipoEvento.PropertyView && d.ImovelId is null) throw DomainException.Validacao("Imóvel não informado.");
        if (d.Detalhes is { } detalhes && (detalhes.Count > 10 || detalhes.Any(x => x.Key.Length > 40 || x.Value?.Length > 200)))
            throw DomainException.Validacao("Detalhes do evento grandes demais.");

        Id = Guid.CreateVersion7();
        Tipo = d.Tipo;
        ImovelId = d.ImovelId;
        Pagina = Cortar(d.Pagina, 500);
        VisitanteId = d.VisitanteId!;
        UtmSource = Cortar(d.UtmSource, 120);
        UtmMedium = Cortar(d.UtmMedium, 120);
        UtmCampaign = Cortar(d.UtmCampaign, 200);
        Referrer = Cortar(d.Referrer, 200);
        Detalhes = d.Detalhes is null ? null : new Dictionary<string, string?>(d.Detalhes);
        Dispositivo = dispositivo;
        CriadoEm = agora;
    }

    private static string? Cortar(string? texto, int tamanho)
    {
        var t = texto?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        return t.Length > tamanho ? t[..tamanho] : t;
    }

    [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
    private static partial Regex VisitanteValido();
}
