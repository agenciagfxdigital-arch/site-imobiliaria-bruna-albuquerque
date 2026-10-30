using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

[Route("api/[controller]")]
public sealed class BairroController(BairroService bairros) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Salvar([FromBody] SalvarBairroRequest request) => RespostaParaStatus(await bairros.Salvar(request));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id) => RespostaParaStatus(await bairros.Excluir(id));
}
