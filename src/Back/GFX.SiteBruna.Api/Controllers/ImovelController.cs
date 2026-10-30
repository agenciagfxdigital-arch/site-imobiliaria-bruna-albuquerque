using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>Imóveis no painel (com dados internos). O site público lê pelo <see cref="SiteController"/>.</summary>
[Route("api/[controller]")]
public sealed class ImovelController(ImovelService imoveis) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca, [FromQuery] StatusImovel? status, [FromQuery] Finalidade? finalidade, [FromQuery] Guid? bairroId) =>
        RespostaParaStatus(await imoveis.Listar(new FiltroDeImoveis(busca, status, finalidade, bairroId)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id) => RespostaParaStatus(await imoveis.Obter(id));

    /// <summary>Cria (sem id) ou atualiza.</summary>
    [HttpPost]
    public async Task<IActionResult> Salvar([FromBody] SalvarImovelRequest request) => RespostaParaStatus(await imoveis.Salvar(request));

    [HttpPut("{id:guid}/fotos")]
    public async Task<IActionResult> SalvarFotos(Guid id, [FromBody] IReadOnlyList<FotoDesejada> fotos) =>
        RespostaParaStatus(await imoveis.SalvarFotos(id, fotos));

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] StatusImovel status) =>
        RespostaParaStatus(await imoveis.AlterarStatus(id, status));

    [HttpPut("{id:guid}/destaque")]
    public async Task<IActionResult> AlterarDestaque(Guid id, [FromBody] bool destaque) =>
        RespostaParaStatus(await imoveis.AlterarDestaque(id, destaque));

    [HttpPost("{id:guid}/duplicar")]
    public async Task<IActionResult> Duplicar(Guid id) => RespostaParaStatus(await imoveis.Duplicar(id));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id) => RespostaParaStatus(await imoveis.Excluir(id));
}
