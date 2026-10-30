using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.AspNetCore.Mvc;

namespace GFX.SiteBruna.Api.Controllers;

/// <summary>Configurações do site e textos das páginas (painel).</summary>
[Route("api/[controller]")]
public sealed class ConteudoController(ConteudoService conteudo) : ApiControllerBase
{
    [HttpPut("configuracoes")]
    public async Task<IActionResult> SalvarConfiguracoes([FromBody] DadosDasConfiguracoes dados) =>
        RespostaParaStatus(await conteudo.SalvarConfiguracoes(dados));

    [HttpPut("paginas/home")]
    public async Task<IActionResult> SalvarHome([FromBody] ConteudoHome pagina) => RespostaParaStatus(await conteudo.SalvarHome(pagina));

    [HttpPut("paginas/sobre")]
    public async Task<IActionResult> SalvarSobre([FromBody] ConteudoSobre pagina) => RespostaParaStatus(await conteudo.SalvarSobre(pagina));

    [HttpPut("paginas/mcmv")]
    public async Task<IActionResult> SalvarMcmv([FromBody] ConteudoMcmv pagina) => RespostaParaStatus(await conteudo.SalvarMcmv(pagina));
}
