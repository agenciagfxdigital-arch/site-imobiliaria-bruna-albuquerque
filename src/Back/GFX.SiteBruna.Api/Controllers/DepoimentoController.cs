using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

[Route("api/[controller]")]
public sealed class DepoimentoController(DepoimentoService depoimentos) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Salvar([FromBody] SalvarDepoimentoRequest request) =>
        RespostaParaStatus(await depoimentos.Salvar(request));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id) => RespostaParaStatus(await depoimentos.Excluir(id));
}
