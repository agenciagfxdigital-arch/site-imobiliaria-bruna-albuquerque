using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Aplicacao;

public class IntegracaoServiceTests
{
    private readonly Mundo _m = new();
    private readonly Imovel _ba103;

    public IntegracaoServiceTests()
    {
        _ba103 = _m.CriarImovel(Mundo.Venda(), quantasFotos: 2, bairro: "Centro");
    }

    private async Task<Operador> Op(string telefone = Mundo.TelefoneBruna) => (await _m.Integracao.OperadorAutorizado(telefone))!;

    [Fact]
    public async Task Autoriza_telefone_cadastrado_com_ou_sem_mascara_e_recusa_os_demais()
    {
        Assert.Equal("Bruna", (await _m.Integracao.OperadorAutorizado("+55 (49) 99000-0000"))!.Nome);
        Assert.Null(await _m.Integracao.OperadorAutorizado("5549988887777"));
        Assert.Null(await _m.Integracao.OperadorAutorizado(Mundo.TelefoneDesativado));
        Assert.Null(await _m.Integracao.OperadorAutorizado("123"));
    }

    [Fact]
    public async Task Alteracao_de_preco_nada_muda_antes_da_confirmacao_e_depois_aplica_e_audita()
    {
        var op = await Op();
        var proposta = await _m.Integracao.ProporAlteracao(op, await Imovel(), new CamposDoImovel { Preco = 349_000 });

        Assert.Contains("• Preço: R$ 365.000 → *R$ 349.000* (trezentos e quarenta e nove mil reais)", proposta.Resumo);
        Assert.Equal(365_000, _m.Imovel(_ba103.Codigo).Preco);

        var invalidacoesAntes = _m.Invalidacoes;
        await _m.Integracao.Confirmar(await Op(), proposta.AcaoId);

        Assert.Equal(349_000, _m.Imovel(_ba103.Codigo).Preco);
        Assert.True(_m.Invalidacoes > invalidacoesAntes);
        var registro = Assert.Single(_m.AuditoriaGravada, r => r.Operacao == RegistroDeAuditoria.ConfirmarAcao);
        Assert.Equal(["preco"], registro.CamposAlterados);
        Assert.Equal("Bruna", _m.InternosDe(_ba103.Id)!.AtualizadoPor);
    }

    [Fact]
    public async Task Dois_toques_em_confirmar_aplicam_uma_vez_so()
    {
        var proposta = await _m.Integracao.ProporStatus(await Op(), _ba103.Codigo, StatusImovel.Vendido);
        await _m.Integracao.Confirmar(await Op(), proposta.AcaoId);

        var erro = await Assert.ThrowsAsync<DomainException>(() => _m.Integracao.Confirmar(Op().Result, proposta.AcaoId));
        Assert.Equal(CodigoErro.Conflito, erro.Codigo);
        Assert.Single(_m.AuditoriaGravada, r => r.Operacao == RegistroDeAuditoria.ConfirmarAcao);
    }

    [Fact]
    public async Task Outro_operador_nao_confirma_a_acao_da_Bruna()
    {
        var proposta = await _m.Integracao.ProporStatus(await Op(), _ba103.Codigo, StatusImovel.Pausado);

        var erro = await Assert.ThrowsAsync<DomainException>(async () =>
            await _m.Integracao.Confirmar(await Op(Mundo.TelefoneAgencia), proposta.AcaoId));

        Assert.Equal(CodigoErro.NaoEncontrado, erro.Codigo);
        Assert.Equal(StatusImovel.Ativo, _m.Imovel(_ba103.Codigo).Status);
    }

    [Fact]
    public async Task Confirmacao_expira_em_15_minutos()
    {
        var proposta = await _m.Integracao.ProporStatus(await Op(), _ba103.Codigo, StatusImovel.Pausado);
        _m.Avancar(TimeSpan.FromMinutes(16));

        var erro = await Assert.ThrowsAsync<DomainException>(async () => await _m.Integracao.Confirmar(await Op(), proposta.AcaoId));
        Assert.Equal(CodigoErro.Expirada, erro.Codigo);
    }

    [Fact]
    public async Task Nao_aplica_se_o_imovel_foi_editado_no_painel_depois_da_proposta()
    {
        var proposta = await _m.Integracao.ProporAlteracao(await Op(), await Imovel(), new CamposDoImovel { Preco = 349_000 });
        _m.Avancar(TimeSpan.FromMinutes(1));
        await _m.ImovelService.AlterarDestaque(_ba103.Id, true);
        _m.Descartar(); // nova requisição

        var erro = await Assert.ThrowsAsync<DomainException>(async () => await _m.Integracao.Confirmar(await Op(), proposta.AcaoId));

        Assert.Equal(CodigoErro.Conflito, erro.Codigo);
        Assert.Equal(365_000, _m.Imovel(_ba103.Codigo).Preco);
    }

    [Fact]
    public async Task Cancelar_nao_altera_nada_e_invalida_a_acao()
    {
        var proposta = await _m.Integracao.ProporStatus(await Op(), _ba103.Codigo, StatusImovel.Pausado);
        var cancelada = await _m.Integracao.Cancelar(await Op(), proposta.AcaoId);

        Assert.Equal("Cancelado. Nada foi alterado.", cancelada.Mensagem);
        Assert.Equal(StatusImovel.Ativo, _m.Imovel(_ba103.Codigo).Status);
        await Assert.ThrowsAsync<DomainException>(async () => await _m.Integracao.Confirmar(await Op(), proposta.AcaoId));
    }

    [Fact]
    public async Task Edita_rascunho_direto_mas_nao_um_imovel_publicado()
    {
        var rascunho = _m.CriarImovel(Mundo.Venda("Casa nova", StatusImovel.Rascunho));
        var r = await _m.Integracao.AtualizarRascunho(await Op(), (await _m.Imoveis.GetById(rascunho.Id))!, new CamposDoImovel { Quartos = 4 });
        Assert.Equal(1, r.CamposAlterados);
        Assert.Equal(4, _m.Imovel(rascunho.Codigo).Quartos);

        var erro = await Assert.ThrowsAsync<DomainException>(async () =>
            await _m.Integracao.AtualizarRascunho(await Op(), await Imovel(), new CamposDoImovel { Quartos = 4 }));
        Assert.Equal(CodigoErro.Conflito, erro.Codigo);
    }

    [Fact]
    public async Task Endereco_o_resumo_mostra_o_valor_gravado_antes()
    {
        await _m.ImovelService.Salvar(new(_ba103.Id, _m.Imovel(_ba103.Codigo).ParaDados(null) with { Endereco = "Rua A, 100" }));
        _m.Descartar();

        var proposta = await _m.Integracao.ProporAlteracao(await Op(), await Imovel(), new CamposDoImovel { Endereco = "Rua B, 200" });

        Assert.Contains("• Endereço (interno): Rua A, 100 → *Rua B, 200*", proposta.Resumo);
    }

    [Fact]
    public async Task Recusa_propostas_invalidas()
    {
        var op = await Op();
        await Assert.ThrowsAsync<DomainException>(() => _m.Integracao.ProporAlteracao(op, _ba103, new CamposDoImovel()));
        await Assert.ThrowsAsync<DomainException>(() => _m.Integracao.ProporAlteracao(op, _ba103, new CamposDoImovel { Preco = 365_000 }));
        await Assert.ThrowsAsync<DomainException>(() => _m.Integracao.ProporAlteracao(op, _ba103, new CamposDoImovel { Titulo = "Ap" }));
        await Assert.ThrowsAsync<DomainException>(() => _m.Integracao.ProporPublicacao(op, _ba103.Codigo));
        await Assert.ThrowsAsync<DomainException>(() => _m.Integracao.ProporStatus(op, _ba103.Codigo, StatusImovel.Alugado));
        await Assert.ThrowsAsync<DomainException>(() => _m.Integracao.ProporDestaque(op, _ba103.Codigo, false));
        Assert.Empty(_m.AcoesGravadas);
    }

    [Fact]
    public async Task Desfazer_volta_o_preco_anterior_tambem_com_confirmacao()
    {
        var proposta = await _m.Integracao.ProporAlteracao(await Op(), await Imovel(), new CamposDoImovel { Preco = 349_000 });
        await _m.Integracao.Confirmar(await Op(), proposta.AcaoId);
        _m.Descartar();

        var desfazer = await _m.Integracao.ProporDesfazer(await Op());
        Assert.Contains("Desfazer a última alteração", desfazer.Resumo);
        Assert.Equal(349_000, _m.Imovel(_ba103.Codigo).Preco);

        await _m.Integracao.Confirmar(await Op(), desfazer.AcaoId);
        Assert.Equal(365_000, _m.Imovel(_ba103.Codigo).Preco);
    }

    [Fact]
    public async Task Desfazer_volta_status_e_destaque()
    {
        var status = await _m.Integracao.ProporStatus(await Op(), _ba103.Codigo, StatusImovel.Vendido);
        await _m.Integracao.Confirmar(await Op(), status.AcaoId);
        _m.Descartar();

        await _m.Integracao.Confirmar(await Op(), (await _m.Integracao.ProporDesfazer(await Op())).AcaoId);

        var imovel = _m.Imovel(_ba103.Codigo);
        Assert.Equal(StatusImovel.Ativo, imovel.Status);
        Assert.Null(imovel.VendidoEm);
    }

    [Fact]
    public async Task Desfazer_so_dentro_de_30_minutos()
    {
        var proposta = await _m.Integracao.ProporStatus(await Op(), _ba103.Codigo, StatusImovel.Pausado);
        await _m.Integracao.Confirmar(await Op(), proposta.AcaoId);
        _m.Avancar(TimeSpan.FromMinutes(31));

        var erro = await Assert.ThrowsAsync<DomainException>(async () => await _m.Integracao.ProporDesfazer(await Op()));
        Assert.Contains("30 minutos", erro.Message);
    }

    [Fact]
    public async Task Selecionar_rascunho_passa_a_receber_as_fotos_e_publicado_nao_muda_o_imovel_atual()
    {
        var rascunho = _m.CriarImovel(Mundo.Venda("Casa nova", StatusImovel.Rascunho));
        await _m.Integracao.SelecionarImovel(await Op(), rascunho.Codigo);
        await _m.Integracao.SelecionarImovel(await Op(), _ba103.Codigo);

        Assert.Equal(rascunho.Id, _m.Operador(Mundo.TelefoneBruna).ImovelAtualId);
    }

    [Fact]
    public async Task Sessao_lista_as_acoes_pendentes()
    {
        await _m.Integracao.ProporStatus(await Op(), _ba103.Codigo, StatusImovel.Pausado);
        var sessao = await _m.Integracao.Sessao(await Op());
        Assert.Single(sessao.Pendentes);
    }

    private async Task<Imovel> Imovel() => (await _m.Imoveis.GetById(_ba103.Id))!;
}
