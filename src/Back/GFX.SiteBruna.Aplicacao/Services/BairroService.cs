using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Aplicacao.Services;

public sealed class BairroService(IBairroRepository bairros, ICacheDoSite cache, IUnitOfWork unitOfWork)
{
    private readonly IBairroRepository _bairros = bairros;
    private readonly ICacheDoSite _cache = cache;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Task<ApiResponse<IReadOnlyList<BairroResponse>>> Listar() =>
        Resposta.De(async () => (IReadOnlyList<BairroResponse>)[.. (await _bairros.GetAll()).Select(BairroResponse.De)]);

    public Task<ApiResponse<BairroResponse>> Salvar(SalvarBairroRequest request) =>
        Resposta.De(async () =>
        {
            Bairro bairro;
            if (request.Id is { } id)
            {
                bairro = await Exigir(id);
                bairro.Renomear(request.Nome);
            }
            else
            {
                bairro = new Bairro(request.Nome);
                _bairros.Add(bairro);
            }
            if (await _bairros.ExistsBySlug(bairro.Slug, bairro.Id))
                throw new DomainException(CodigoErro.Conflito, "Esse bairro já está cadastrado.");
            await GravarEAvisar();
            return BairroResponse.De(bairro);
        });

    public Task<ApiResponse<bool>> Excluir(Guid id) =>
        Resposta.De(async () =>
        {
            var bairro = await Exigir(id);
            var imoveis = await _bairros.ContarImoveis(id);
            if (imoveis > 0)
                throw new DomainException(CodigoErro.Conflito,
                    $"Esse bairro está em {imoveis} imóvel(is). Troque o bairro deles antes de excluir.");
            _bairros.Delete(bairro);
            await GravarEAvisar();
        });

    private async Task<Bairro> Exigir(Guid id) =>
        await _bairros.GetById(id) ?? throw new DomainException(CodigoErro.NaoEncontrado, "Bairro não encontrado.");

    private async Task GravarEAvisar()
    {
        await _unitOfWork.Commit();
        await _cache.Invalidar();
    }
}
