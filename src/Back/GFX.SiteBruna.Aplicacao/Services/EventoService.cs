using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Eventos de navegação para as métricas do painel (o clique no WhatsApp é a conversão principal).</summary>
public sealed class EventoService(IEventoRepository eventos, IRelogio relogio)
{
    private readonly IEventoRepository _eventos = eventos;
    private readonly IRelogio _relogio = relogio;

    /// <summary>
    /// Devolve se o evento foi gravado. Robôs e pré-visualizações de link (inclusive a do WhatsApp) são ignorados,
    /// assim como a segunda visualização do mesmo imóvel pelo mesmo visitante.
    /// </summary>
    public Task<ApiResponse<bool>> Registrar(DadosDoEvento dados, string? userAgent) =>
        Resposta.De(async () =>
            !Navegador.EhRobo(userAgent)
            && await _eventos.Registrar(new Evento(dados, Navegador.DispositivoDe(userAgent), _relogio.Agora)));
}
