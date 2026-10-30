using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Aplicacao.Services;

public sealed class FaqService(IFaqRepository faqs, ICacheDoSite cache, IUnitOfWork unitOfWork, IRelogio relogio)
{
    private readonly IFaqRepository _faqs = faqs;
    private readonly ICacheDoSite _cache = cache;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;

    public Task<ApiResponse<IReadOnlyList<FaqResponse>>> Listar() =>
        Resposta.De(async () => (IReadOnlyList<FaqResponse>)[.. (await _faqs.GetAll()).Select(FaqResponse.De)]);

    public Task<ApiResponse<FaqResponse>> Salvar(SalvarFaqRequest request) =>
        Resposta.De(async () =>
        {
            Faq faq;
            if (request.Id is { } id)
            {
                faq = await Exigir(id);
                faq.Atualizar(request.Pergunta, request.Resposta, request.Ordem);
            }
            else
            {
                faq = new Faq(request.Pergunta, request.Resposta, request.Ordem, _relogio.Agora);
                _faqs.Add(faq);
            }
            await GravarEAvisar();
            return FaqResponse.De(faq);
        });

    public Task<ApiResponse<bool>> Excluir(Guid id) =>
        Resposta.De(async () =>
        {
            _faqs.Delete(await Exigir(id));
            await GravarEAvisar();
        });

    /// <summary>Nova ordem a partir da lista arrastada no painel.</summary>
    public Task<ApiResponse<bool>> Reordenar(IReadOnlyList<Guid> ids) =>
        Resposta.De(async () =>
        {
            Faq.Reordenar(await _faqs.GetAll(), ids);
            await GravarEAvisar();
        });

    private async Task<Faq> Exigir(Guid id) =>
        await _faqs.GetById(id) ?? throw new DomainException(CodigoErro.NaoEncontrado, "Pergunta não encontrada.");

    private async Task GravarEAvisar()
    {
        await _unitOfWork.Commit();
        await _cache.Invalidar();
    }
}
