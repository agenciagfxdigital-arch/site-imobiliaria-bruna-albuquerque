using GFX.SiteBruna.Front.Web.Autenticacao;
using GFX.SiteBruna.Front.Web.DTOs;
using GFX.SiteBruna.Front.Web.DTOs.Metricas;

namespace GFX.SiteBruna.Front.Web.Services;

/// <summary>Números do início do painel (<c>/api/Metricas</c>).</summary>
public sealed class MetricasApi(HttpClient http, SessaoDoPainel sessao) : ApiClientBase(http, sessao)
{
    public Task<ApiResponse<MetricasResponse>> Obter(PeriodoEscolhido periodo) => Get<MetricasResponse>(
        periodo is { De: { } de, Ate: { } ate }
            ? $"api/Metricas?de={de:yyyy-MM-dd}&ate={ate:yyyy-MM-dd}"
            : $"api/Metricas?dias={periodo.Dias}");
}
