using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Primeira carga do conteúdo do site. Cada lista só é preenchida se estiver vazia: pode rodar de novo sem duplicar.</summary>
public sealed class ConteudoInicialService(
    IBairroRepository bairros, IFaqRepository faqs, IDepoimentoRepository depoimentos, ICacheDoSite cache, IUnitOfWork unitOfWork, IRelogio relogio)
{
    private readonly IBairroRepository _bairros = bairros;
    private readonly IFaqRepository _faqs = faqs;
    private readonly IDepoimentoRepository _depoimentos = depoimentos;
    private readonly ICacheDoSite _cache = cache;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;

    public Task<ApiResponse<ConteudoInicialResponse>> Carregar() =>
        Resposta.De(async () =>
        {
            var (bairros, perguntas, depoimentos) = (0, 0, 0);
            var agora = _relogio.Agora;

            if ((await _bairros.GetAll()).Count == 0)
                foreach (var nome in ConteudoInicial.Bairros)
                {
                    _bairros.Add(new Bairro(nome));
                    bairros++;
                }

            if ((await _faqs.GetAll()).Count == 0)
                foreach (var (pergunta, resposta) in ConteudoInicial.Perguntas)
                    _faqs.Add(new Faq(pergunta, resposta, ++perguntas, agora));

            if ((await _depoimentos.GetAll(somenteDaHome: false)).Count == 0)
                foreach (var (nome, texto) in ConteudoInicial.Depoimentos)
                    _depoimentos.Add(new Depoimento(nome, texto, null, true, ++depoimentos, agora));

            await _unitOfWork.Commit();
            await _cache.Invalidar();
            return new ConteudoInicialResponse(bairros, perguntas, depoimentos);
        });
}
