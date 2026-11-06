using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Metricas;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Início do painel: visitas, cliques no WhatsApp, imóveis mais vistos e o que o público procura.</summary>
public sealed class MetricasService(IMetricasRepository metricas, IRelogio relogio)
{
    private const int LimiteDeImoveis = 15;
    private const int LimiteDeValores = 8;

    private readonly IMetricasRepository _metricas = metricas;
    private readonly IRelogio _relogio = relogio;

    public Task<ApiResponse<MetricasResponse>> Obter(int? dias, DateOnly? de, DateOnly? ate) =>
        Resposta.De(async () =>
        {
            var periodo = PeriodoDasMetricas.Escolher(dias, de, ate, _relogio.Agora);
            var (inicio, fim) = (periodo.Inicio, periodo.Fim);

            var atual = await _metricas.Totais(inicio, fim);
            var anterior = await _metricas.Totais(periodo.InicioDoAnterior, inicio);
            var cliques = (await _metricas.CliquesPorDia(inicio, fim)).ToDictionary(d => d.Dia, d => d.Total);
            var imoveis = await _metricas.ImoveisMaisVistos(inicio, fim, LimiteDeImoveis);
            var finalidades = await _metricas.ValoresDoFiltro(inicio, fim, "finalidade", LimiteDeValores);

            return new MetricasResponse(
                periodo.De,
                periodo.Ate,
                IndicadorResponse.De(atual.Visitantes, anterior.Visitantes),
                IndicadorResponse.De(atual.VisualizacoesDeImoveis, anterior.VisualizacoesDeImoveis),
                IndicadorResponse.De(atual.CliquesNoWhatsapp, anterior.CliquesNoWhatsapp),
                [.. periodo.Dias.Select(dia => new TotalDoDia(dia, cliques.GetValueOrDefault(dia)))],
                [.. imoveis.Select(ImovelNasMetricasResponse.De)],
                await _metricas.ValoresDoFiltro(inicio, fim, "bairro_nome", LimiteDeValores),
                await _metricas.ValoresDoFiltro(inicio, fim, "tipo", LimiteDeValores),
                await _metricas.ValoresDoFiltro(inicio, fim, "faixa", LimiteDeValores),
                Buscas(finalidades, Finalidade.Venda),
                Buscas(finalidades, Finalidade.Aluguel));
        });

    private static int Buscas(IReadOnlyList<TotalDoFiltro> finalidades, Finalidade finalidade) =>
        finalidades.FirstOrDefault(f => f.Valor.Equals(finalidade.ToString(), StringComparison.OrdinalIgnoreCase))?.Total ?? 0;
}
