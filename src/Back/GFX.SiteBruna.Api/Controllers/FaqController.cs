using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

[Route("api/[controller]")]
public sealed class FaqController(FaqService faqs) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Salvar([FromBody] SalvarFaqRequest request) => RespostaParaStatus(await faqs.Salvar(request));

    [HttpPut("ordem")]
    public async Task<IActionResult> Reordenar([FromBody] IReadOnlyList<Guid> ids) => RespostaParaStatus(await faqs.Reordenar(ids));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id) => RespostaParaStatus(await faqs.Excluir(id));
}
