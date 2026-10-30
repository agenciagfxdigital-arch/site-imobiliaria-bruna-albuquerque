using System.Text.RegularExpressions;
using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Aplicacao.DTOs.Integracao;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.Services;

/// <summary>
/// Casos de uso da integração pelo WhatsApp. É <c>internal</c>: só o <see cref="AssistenteService"/> chama, e a Api não
/// tem como expor a confirmação de uma ação por outro caminho. Lança <c>DomainException</c>, que o assistente
/// transforma em resposta para a Bruna.
/// <list type="bullet">
/// <item>todo caso de uso recebe um operador já autorizado (telefone cadastrado e ativo);</item>
/// <item>rascunhos são criados e editados direto (não aparecem no site);</item>
/// <item>o que aparece no site só muda por ação pendente + confirmação, executada por código;</item>
/// <item>nada é excluído pela integração; toda escrita fica na auditoria.</item>
/// </list>
/// </summary>
internal sealed partial class IntegracaoService(
    IOperadorRepository operadores,
    IAcaoPendenteRepository acoes,
    IAuditoriaRepository auditoria,
    IImovelRepository imoveis,
    IFotoRepository fotos,
    IDadosInternosRepository internos,
    IBairroRepository bairros,
    ICacheDoSite cache,
    IUnitOfWork unitOfWork,
    IRelogio relogio,
    IEnderecosDoSite enderecos,
    ImovelService imovelService)
{
    private readonly IOperadorRepository _operadores = operadores;
    private readonly IAcaoPendenteRepository _acoes = acoes;
    private readonly IAuditoriaRepository _auditoria = auditoria;
    private readonly IImovelRepository _imoveis = imoveis;
    private readonly IFotoRepository _fotos = fotos;
    private readonly IDadosInternosRepository _internos = internos;
    private readonly IBairroRepository _bairros = bairros;
    private readonly ICacheDoSite _cache = cache;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRelogio _relogio = relogio;
    private readonly IEnderecosDoSite _enderecos = enderecos;
    private readonly ImovelService _imovelService = imovelService;

    // ---------------------------------------------------------------------------------------------
    // Operador e leitura
    // ---------------------------------------------------------------------------------------------

    /// <summary>Operador do telefone, se estiver cadastrado e ativo; <c>null</c> caso contrário.</summary>
    public async Task<Operador?> OperadorAutorizado(string telefone)
    {
        try
        {
            return Operador.GarantirAutorizado(await _operadores.GetByTelefone(Telefone.Normalizar(telefone)));
        }
        catch (DomainException e) when (e.Codigo == CodigoErro.NaoAutorizado)
        {
            return null;
        }
    }

    public async Task<SessaoDoOperador> Sessao(Operador op)
    {
        var imovel = op.ImovelAtualId is { } id ? await _imoveis.GetById(id) : null;
        var totalDeFotos = imovel is null ? 0 : (await _fotos.GetDoImovel(imovel.Id)).Count;
        return new SessaoDoOperador(imovel, totalDeFotos, await _acoes.GetPendentes(op.Id, _relogio.Agora));
    }

    public async Task<IReadOnlyList<Bairro>> Bairros() => await _bairros.GetAll();

    public async Task<Imovel> ExigirImovelPorCodigo(string? codigo)
    {
        var limpo = codigo?.Trim().ToUpperInvariant() ?? "";
        if (!CodigoValido().IsMatch(limpo)) throw DomainException.Validacao("Informe o código do imóvel (ex.: BA103).");
        return await _imoveis.GetByCodigo(limpo)
            ?? throw new DomainException(CodigoErro.NaoEncontrado, $"Não existe imóvel {limpo}.", await _imoveis.GetCodigosRecentes(3));
    }

    /// <summary>Escolhe o imóvel que recebe as próximas fotos (só rascunho: publicado muda pelo painel).</summary>
    public async Task<Imovel> SelecionarImovel(Operador op, string codigo)
    {
        var imovel = await ExigirImovelPorCodigo(codigo);
        if (imovel.Status != StatusImovel.Rascunho) return imovel;
        op.DefinirImovelAtual(imovel.Id);
        await _unitOfWork.Commit();
        return imovel;
    }

    // ---------------------------------------------------------------------------------------------
    // Rascunhos (não aparecem no site)
    // ---------------------------------------------------------------------------------------------

    public async Task<RascunhoSalvo> CriarRascunho(Operador op, CamposDoImovel campos)
    {
        var imovel = await _imovelService.Salvar(null, campos.ParaNovoRascunho(), OrigemImovel.Whatsapp, op.Nome);
        op.DefinirImovelAtual(imovel.Id);
        _auditoria.Add(RegistroDeAuditoria.Sucedido(op.Id, RegistroDeAuditoria.CriarRascunho, imovel.Id, _relogio.Agora,
            detalhe: imovel.Codigo));
        await _unitOfWork.Commit();
        return new RascunhoSalvo(imovel.Codigo, 0, Sanidade.Avisos(imovel.ParaDados(null)), _enderecos.PreviaNoPainel(imovel.Id));
    }

    public async Task<RascunhoSalvo> AtualizarRascunho(Operador op, Imovel imovel, CamposDoImovel campos)
    {
        imovel.GarantirRascunho();
        campos.Validar();
        var antes = imovel.ParaDados(await _internos.GetByImovel(imovel.Id));
        var depois = campos.AplicarEm(antes);
        var mudancas = Diferencas.Entre(antes, depois);

        await _imovelService.Salvar(imovel.Id, depois, OrigemImovel.Whatsapp, op.Nome);
        op.DefinirImovelAtual(imovel.Id);
        _auditoria.Add(RegistroDeAuditoria.Sucedido(op.Id, RegistroDeAuditoria.AtualizarRascunho, imovel.Id, _relogio.Agora, antes, depois));
        await _unitOfWork.Commit();
        return new RascunhoSalvo(imovel.Codigo, mudancas.Count, Sanidade.Avisos(depois), _enderecos.PreviaNoPainel(imovel.Id));
    }

    /// <summary>Foto recebida pelo WhatsApp. Só em rascunho (fotos de imóvel publicado: pelo painel).</summary>
    public async Task<FotoAdicionada> AdicionarFoto(Operador op, string? codigo, byte[] conteudo)
    {
        var imovel = codigo is not null
            ? await ExigirImovelPorCodigo(codigo)
            : (op.ImovelAtualId is { } id ? await _imoveis.GetById(id) : null)
              ?? throw DomainException.Validacao("Não sei de qual imóvel é. Informe o código (ex.: BA103).");
        imovel.GarantirRascunho();

        var foto = await _imovelService.AdicionarFoto(imovel.Id, conteudo);
        op.DefinirImovelAtual(imovel.Id);
        _auditoria.Add(RegistroDeAuditoria.Sucedido(op.Id, RegistroDeAuditoria.AdicionarFoto, imovel.Id, _relogio.Agora,
            detalhe: foto.Imagem.CaminhoNoStorage));
        await _unitOfWork.Commit();
        return foto;
    }

    // ---------------------------------------------------------------------------------------------
    // Propostas (só valem depois da confirmação)
    // ---------------------------------------------------------------------------------------------

    public async Task<PropostaFeita> ProporPublicacao(Operador op, string codigo)
    {
        var imovel = await ExigirImovelPorCodigo(codigo);
        imovel.GarantirQuePodeSerPublicado();
        var avisos = Sanidade.Avisos(imovel.ParaDados(null));
        var totalDeFotos = (await _fotos.GetDoImovel(imovel.Id)).Count;
        var bairro = imovel.BairroId is { } bairroId ? (await _bairros.GetById(bairroId))?.Nome : null;
        var resumo = ResumosDeAcao.Publicacao(imovel, bairro, totalDeFotos, avisos);
        return await Registrar(AcaoPendente.Publicar(op.Id, imovel, resumo, _relogio.Agora), avisos);
    }

    public async Task<PropostaFeita> ProporAlteracao(Operador op, Imovel imovel, CamposDoImovel campos)
    {
        campos.Validar();
        await _imovelService.NomeDoBairro(campos.BairroId);
        var atual = imovel.ParaDados(await _internos.GetByImovel(imovel.Id));
        var proposto = campos.AplicarEm(atual);
        Imovel.Validar(proposto); // o resultado final precisa ser válido
        var diferencas = Diferencas.Entre(atual, proposto);
        if (diferencas.Count == 0) throw DomainException.Validacao("Esses valores já estão salvos. Nada para alterar.");

        var avisos = Sanidade.Avisos(proposto);
        var resumo = ResumosDeAcao.Alteracao(imovel, diferencas, await NomesDosBairros(), avisos);
        return await Registrar(
            AcaoPendente.AlterarCampos(op.Id, imovel, TipoAcao.Alterar, proposto, diferencas, resumo, _relogio.Agora), avisos);
    }

    public async Task<PropostaFeita> ProporStatus(Operador op, string codigo, StatusImovel novo)
    {
        var imovel = await ExigirImovelPorCodigo(codigo);
        imovel.GarantirNovoStatusValido(novo);
        return await Registrar(AcaoPendente.MudarStatus(op.Id, imovel, novo, ResumosDeAcao.Status(imovel, novo), _relogio.Agora), []);
    }

    public async Task<PropostaFeita> ProporDestaque(Operador op, string codigo, bool destaque)
    {
        var imovel = await ExigirImovelPorCodigo(codigo);
        var resumo = ResumosDeAcao.Destaque(imovel, destaque);
        return await Registrar(AcaoPendente.MudarDestaque(op.Id, imovel, destaque, resumo, _relogio.Agora), []);
    }

    /// <summary>Volta a última alteração confirmada pelo operador (também precisa de confirmação).</summary>
    public async Task<PropostaFeita> ProporDesfazer(Operador op)
    {
        var agora = _relogio.Agora;
        var registro = RegistroDeAuditoria.VerificarDesfazer(await _auditoria.GetUltimaConfirmada(op.Id), op.Id, agora);
        var imovel = await _imoveis.GetById(registro.ImovelId!.Value)
            ?? throw new DomainException(CodigoErro.NaoEncontrado, "O imóvel dessa alteração não existe mais.");
        var atual = imovel.ParaDados(await _internos.GetByImovel(imovel.Id));
        var proposto = registro.EstadoParaDesfazer(atual);
        var diferencas = Diferencas.Entre(atual, proposto);
        if (diferencas.Count == 0) throw new DomainException(CodigoErro.Conflito, "O imóvel já está como antes dessa alteração.");

        var resumo = ResumosDeAcao.Desfazer(imovel, diferencas, await NomesDosBairros());
        return await Registrar(AcaoPendente.AlterarCampos(op.Id, imovel, TipoAcao.Desfazer, proposto, diferencas, resumo, agora), []);
    }

    // ---------------------------------------------------------------------------------------------
    // Confirmação (botão "Confirmar" ou "sim" com uma única confirmação aberta)
    // ---------------------------------------------------------------------------------------------

    public async Task<AcaoConcluida> Confirmar(Operador op, Guid acaoId)
    {
        var agora = _relogio.Agora;
        var acao = AcaoPendente.Verificar(await _acoes.GetById(acaoId), op.Id, agora);
        var imovel = await _imoveis.GetById(acao.ImovelId)
            ?? throw new DomainException(CodigoErro.NaoEncontrado, "O imóvel desta ação não existe mais.");
        acao.GarantirImovelInalterado(imovel);
        if (!await _acoes.ReservarConfirmacao(acao.Id, agora))
            throw new DomainException(CodigoErro.Conflito, "Essa ação já foi confirmada.");

        try
        {
            var dadosInternos = await _internos.GetByImovel(imovel.Id);
            if (dadosInternos is null)
            {
                dadosInternos = new DadosInternosImovel(imovel.Id, agora);
                _internos.Add(dadosInternos);
            }
            var antes = imovel.ParaDados(dadosInternos);
            var nomeDoBairro = await _imovelService.NomeDoBairro(acao.DadosPropostos?.BairroId ?? imovel.BairroId);

            acao.Executar(imovel, dadosInternos, nomeDoBairro, op.Nome, agora);

            _auditoria.Add(RegistroDeAuditoria.Sucedido(op.Id, RegistroDeAuditoria.ConfirmarAcao, imovel.Id, agora,
                antes, imovel.ParaDados(dadosInternos), detalhe: acao.Id.ToString("D")));
            await _unitOfWork.Commit();
        }
        catch (Exception erro)
        {
            // Nada da execução pela metade vai para o banco; a ação volta a ficar pendente para tentar de novo.
            _unitOfWork.Descartar();
            await _acoes.DevolverParaPendente(acao.Id);
            _auditoria.Add(RegistroDeAuditoria.Falhou(op.Id, RegistroDeAuditoria.ConfirmarAcao, imovel.Id, erro.Message, agora,
                acao.Id.ToString("D")));
            await _unitOfWork.Commit();
            throw;
        }
        await _cache.Invalidar();

        var mensagem = acao.Tipo switch
        {
            TipoAcao.Publicar => $"Pronto! O {imovel.Codigo} já está no site.",
            TipoAcao.Alterar => $"Pronto! O site já mostra as alterações no {imovel.Codigo}.",
            TipoAcao.Status => $"Pronto! O {imovel.Codigo} foi atualizado.",
            TipoAcao.Destaque => "Pronto! Destaques da página inicial atualizados.",
            _ => $"Pronto! Desfiz a alteração no {imovel.Codigo}.",
        };
        var visivel = imovel.Status is StatusImovel.Ativo or StatusImovel.Vendido or StatusImovel.Alugado;
        return new AcaoConcluida(acao.Tipo, imovel.Codigo, mensagem,
            visivel ? _enderecos.PaginaDoImovel(imovel.Slug, imovel.Codigo) : null);
    }

    public async Task<AcaoConcluida> Cancelar(Operador op, Guid acaoId)
    {
        var acao = AcaoPendente.Verificar(await _acoes.GetById(acaoId), op.Id, _relogio.Agora);
        acao.Cancelar();
        await _unitOfWork.Commit();
        var codigo = (await _imoveis.GetById(acao.ImovelId))?.Codigo ?? "";
        return new AcaoConcluida(acao.Tipo, codigo, "Cancelado. Nada foi alterado.", null);
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<PropostaFeita> Registrar(AcaoPendente acao, IReadOnlyList<string> avisos)
    {
        _acoes.Add(acao);
        await _unitOfWork.Commit();
        return new PropostaFeita(acao.Id, acao.Resumo, avisos);
    }

    private async Task<Func<Guid, string?>> NomesDosBairros()
    {
        var nomes = (await _bairros.GetAll()).ToDictionary(b => b.Id, b => b.Nome);
        return id => nomes.GetValueOrDefault(id);
    }

    [GeneratedRegex("^[A-Z0-9]{2,20}$")]
    private static partial Regex CodigoValido();
}
