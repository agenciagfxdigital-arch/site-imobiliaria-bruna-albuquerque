using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Interfaces;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>Usuários do painel e login (o token é gerado pelo <see cref="ITokenService"/>).</summary>
public sealed class UsuarioService(
    IUsuarioRepository usuarios, IHasherSenha hasher, ITokenService tokens, IUnitOfWork unitOfWork, IRelogio relogio)
{
    private readonly IUsuarioRepository _usuarios = usuarios;
    private readonly IHasherSenha _hasher = hasher;
    private readonly ITokenService _tokens = tokens;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;

    /// <summary>E-mail ou senha errados, ou usuário desativado: sempre a mesma mensagem.</summary>
    public Task<ApiResponse<SessaoResponse>> Entrar(EntrarRequest request) =>
        Resposta.De(async () =>
        {
            Usuario? encontrado;
            try
            {
                encontrado = await _usuarios.GetByEmail(Usuario.NormalizarEmail(request.Email));
            }
            catch (DomainException)
            {
                encontrado = null; // e-mail em formato inválido: mesmo caminho de "não existe"
            }
            var usuario = Usuario.Entrar(encontrado, request.Senha ?? "", _hasher, _relogio.Agora);
            await _unitOfWork.Commit(); // último acesso e, se for o caso, hash novo
            var resposta = UsuarioResponse.De(usuario);
            var (token, expiraEm) = _tokens.Gerar(resposta);
            return new SessaoResponse(token, expiraEm, resposta);
        });

    /// <summary>Confere a cada requisição se o usuário do token ainda existe e está ativo.</summary>
    public Task<ApiResponse<UsuarioResponse>> Ativo(Guid id) =>
        Resposta.De(async () =>
        {
            var usuario = await _usuarios.GetById(id);
            if (usuario is not { Ativo: true })
                throw new DomainException(CodigoErro.NaoAutorizado, "Sua sessão terminou. Entre de novo.");
            return UsuarioResponse.De(usuario);
        });

    public Task<ApiResponse<IReadOnlyList<UsuarioResponse>>> Listar() =>
        Resposta.De(async () => (IReadOnlyList<UsuarioResponse>)[.. (await _usuarios.GetAll()).Select(UsuarioResponse.De)]);

    public Task<ApiResponse<UsuarioResponse>> Criar(CriarUsuarioRequest request) =>
        Resposta.De(async () =>
        {
            if (await _usuarios.GetByEmail(Usuario.NormalizarEmail(request.Email)) is not null)
                throw new DomainException(CodigoErro.Conflito, "Já existe um usuário com esse e-mail.");
            var usuario = new Usuario(request.Email, request.Nome, request.Senha, _hasher, _relogio.Agora);
            _usuarios.Add(usuario);
            await _unitOfWork.Commit();
            return UsuarioResponse.De(usuario);
        });

    public Task<ApiResponse<bool>> TrocarSenha(Guid usuarioId, TrocarSenhaRequest request) =>
        Resposta.De(async () =>
        {
            var usuario = await _usuarios.GetById(usuarioId)
                ?? throw new DomainException(CodigoErro.NaoEncontrado, "Usuário não encontrado.");
            usuario.TrocarSenha(request.SenhaAtual ?? "", request.NovaSenha ?? "", _hasher);
            await _unitOfWork.Commit();
        });
}
