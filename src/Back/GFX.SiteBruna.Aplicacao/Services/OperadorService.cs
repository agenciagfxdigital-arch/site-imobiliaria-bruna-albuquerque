using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Operadores;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Telefones autorizados a usar o assistente do WhatsApp.</summary>
public sealed class OperadorService(IOperadorRepository operadores, IUnitOfWork unitOfWork, IRelogio relogio)
{
    private readonly IOperadorRepository _operadores = operadores;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;

    public Task<ApiResponse<IReadOnlyList<OperadorResponse>>> Listar() =>
        Resposta.De(async () => (IReadOnlyList<OperadorResponse>)[.. (await _operadores.GetAll()).Select(OperadorResponse.De)]);

    /// <summary>Cadastra o telefone ou, se ele já existir, atualiza o nome e a autorização.</summary>
    public Task<ApiResponse<OperadorResponse>> Salvar(SalvarOperadorRequest request) =>
        Resposta.De(async () =>
        {
            var telefone = Operador.TelefoneDoCadastro(request.Telefone);
            var operador = await _operadores.GetByTelefone(telefone);
            if (operador is null)
            {
                operador = new Operador(telefone, request.Nome ?? "", _relogio.Agora);
                _operadores.Add(operador);
            }
            else operador.Renomear(request.Nome ?? "");

            if (request.Ativo) operador.Ativar();
            else operador.Desativar();
            await _unitOfWork.Commit();
            return OperadorResponse.De(operador);
        });
}
