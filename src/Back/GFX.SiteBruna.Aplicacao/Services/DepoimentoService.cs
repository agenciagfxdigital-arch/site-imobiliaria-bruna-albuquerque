using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Aplicacao.Services;

public sealed class DepoimentoService(IDepoimentoRepository depoimentos, ICacheDoSite cache, IUnitOfWork unitOfWork, IRelogio relogio)
{
    private readonly IDepoimentoRepository _depoimentos = depoimentos;
    private readonly ICacheDoSite _cache = cache;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;

    public Task<ApiResponse<IReadOnlyList<DepoimentoResponse>>> Listar(bool somenteDaHome) =>
        Resposta.De(async () => (IReadOnlyList<DepoimentoResponse>)[.. (await _depoimentos.GetAll(somenteDaHome)).Select(DepoimentoResponse.De)]);

    public Task<ApiResponse<DepoimentoResponse>> Salvar(SalvarDepoimentoRequest r) =>
        Resposta.De(async () =>
        {
            Depoimento depoimento;
            if (r.Id is { } id)
            {
                depoimento = await Exigir(id);
                depoimento.Atualizar(r.Nome, r.Texto, r.FotoUrl, r.ExibirNaHome, r.Ordem);
            }
            else
            {
                depoimento = new Depoimento(r.Nome, r.Texto, r.FotoUrl, r.ExibirNaHome, r.Ordem, _relogio.Agora);
                _depoimentos.Add(depoimento);
            }
            await GravarEAvisar();
            return DepoimentoResponse.De(depoimento);
        });

    public Task<ApiResponse<bool>> Excluir(Guid id) =>
        Resposta.De(async () =>
        {
            _depoimentos.Delete(await Exigir(id));
            await GravarEAvisar();
        });

    private async Task<Depoimento> Exigir(Guid id) =>
        await _depoimentos.GetById(id) ?? throw new DomainException(CodigoErro.NaoEncontrado, "Depoimento não encontrado.");

    private async Task GravarEAvisar()
    {
        await _unitOfWork.Commit();
        await _cache.Invalidar();
    }
}
