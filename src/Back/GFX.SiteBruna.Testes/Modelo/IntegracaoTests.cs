using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class IntegracaoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Op1 = Guid.CreateVersion7();
    private static readonly Guid Op2 = Guid.CreateVersion7();

    /// <summary>O BA103 do seed: apartamento à venda no Centro, R$ 365.000.</summary>
    private static Imovel Ba103(StatusImovel status = StatusImovel.Ativo) => new(new DadosDoImovel
    {
        Codigo = "BA103",
        Finalidade = Finalidade.Venda,
        Tipo = TipoImovel.Apartamento,
        Titulo = "Apartamento 2 quartos com sacada no Centro",
        Preco = 365_000,
        Quartos = 2,
        Status = status,
    }, null, "Centro", OrigemImovel.Painel, Agora.AddHours(-2));

    // --- Telefone -------------------------------------------------------------------------------

    [Theory]
    [InlineData("(49) 99000-0000", "5549990000000")]
    [InlineData("+55 49 99000-0000", "5549990000000")]
    [InlineData("554999000000", "5549999000000")]
    [InlineData("554935660000", "554935660000")]
    public void Normaliza_telefone_com_ou_sem_ddi_e_completa_o_nono_digito(string entrada, string esperado)
    {
        Assert.Equal(esperado, Telefone.Normalizar(entrada));
    }

    [Theory]
    [InlineData("9999")]
    [InlineData(null)]
    public void Recusa_telefone_incompleto_ou_ausente(string? entrada)
    {
        Assert.Equal(CodigoErro.NaoAutorizado, Assert.Throws<DomainException>(() => Telefone.Normalizar(entrada)).Codigo);
    }

    // --- Campos e novo rascunho -----------------------------------------------------------------

    [Fact]
    public void Recusa_garantia_fora_da_lista_e_entrada_vazia()
    {
        Assert.Throws<DomainException>(() => new CamposDoImovel { Garantias = ["Cheque"] }.Validar());
        Assert.Contains("Nenhum campo", Assert.Throws<DomainException>(() => new CamposDoImovel().Validar()).Message);
        new CamposDoImovel { Preco = 349_000, Quartos = 2 }.Validar();
    }

    [Fact]
    public void Novo_rascunho_preenche_o_que_falta_com_valores_neutros()
    {
        var r = new CamposDoImovel
        {
            Finalidade = Finalidade.Venda, Tipo = TipoImovel.Casa, Titulo = "Casa 3 quartos", Preco = 420_000, Quartos = 3,
        }.ParaNovoRascunho();

        Assert.Equal(StatusImovel.Rascunho, r.Status);
        Assert.False(r.Destaque);
        Assert.Equal(420_000, r.Preco);
        Assert.Equal(3, r.Quartos);
        Assert.Equal(0, r.Vagas);
        Assert.Empty(r.Diferenciais);
        Assert.True(r.PrecisaDeCodigoGerado);
    }

    [Fact]
    public void Novo_rascunho_exige_finalidade_tipo_e_titulo()
    {
        Assert.Contains("finalidade", Assert.Throws<DomainException>(() =>
            new CamposDoImovel { Tipo = TipoImovel.Casa, Titulo = "Casa" }.ParaNovoRascunho()).Message);
        Assert.Contains("tipo", Assert.Throws<DomainException>(() =>
            new CamposDoImovel { Finalidade = Finalidade.Venda, Titulo = "Casa" }.ParaNovoRascunho()).Message);
    }

    // --- Avisos --------------------------------------------------------------------------------

    [Fact]
    public void Avisa_valores_fora_do_comum_sem_bloquear()
    {
        Assert.Single(Sanidade.Avisos(new DadosDoImovel { Finalidade = Finalidade.Venda, Preco = 349, Area = 60, Quartos = 2 }));
        Assert.Empty(Sanidade.Avisos(new DadosDoImovel { Finalidade = Finalidade.Aluguel, Aluguel = 1650, Area = 64, Quartos = 2 }));
    }

    // --- Diferenças e resumo -------------------------------------------------------------------

    [Fact]
    public void Lista_so_o_que_mudou()
    {
        var atual = Ba103().ParaDados(null);
        var proposto = new CamposDoImovel { Preco = 349_000, Quartos = 2 }.AplicarEm(atual);

        Assert.Equal([new MudancaCampo("preco", 365_000m, 349_000m)], Diferencas.Entre(atual, proposto));
    }

    [Fact]
    public void Texto_vazio_e_nulo_e_listas_iguais_nao_contam_como_mudanca()
    {
        var atual = Ba103().ParaDados(null) with { Diferenciais = ["Sacada"] };
        var proposto = atual with { ProprietarioNome = null, Diferenciais = ["Sacada"] };

        Assert.Empty(Diferencas.Entre(atual, proposto));
    }

    [Fact]
    public void Resumo_mostra_antes_depois_e_o_valor_por_extenso()
    {
        var texto = ResumosDeAcao.Alteracao(Ba103(), [new MudancaCampo("preco", 365_000m, 349_000m)], _ => null, []);

        Assert.Contains("*BA103 – Apartamento 2 quartos com sacada no Centro*", texto);
        Assert.Contains("• Preço: R$ 365.000 → *R$ 349.000* (trezentos e quarenta e nove mil reais)", texto);
    }

    [Fact]
    public void Resumo_mostra_o_endereco_interno_e_o_bairro_pelo_nome()
    {
        var bairro = Guid.CreateVersion7();
        var texto = ResumosDeAcao.Alteracao(Ba103(),
            [new MudancaCampo("endereco", "Rua A, 100", "Rua B, 200"), new MudancaCampo("bairro_id", null, bairro)],
            id => id == bairro ? "Centro" : null, []);

        Assert.Contains("• Endereço (interno): Rua A, 100 → *Rua B, 200*", texto);
        Assert.Contains("• Bairro: (vazio) → *Centro*", texto);
    }

    // --- Status pelo WhatsApp ------------------------------------------------------------------

    [Fact]
    public void Aceita_mudanca_de_status_valida()
    {
        Ba103().GarantirNovoStatusValido(StatusImovel.Vendido);
    }

    [Fact]
    public void Recusa_status_igual_incompativel_ou_rascunho_direto_no_site()
    {
        Assert.Throws<DomainException>(() => Ba103().GarantirNovoStatusValido(StatusImovel.Ativo));
        Assert.Contains("venda", Assert.Throws<DomainException>(() => Ba103().GarantirNovoStatusValido(StatusImovel.Alugado)).Message);
        Assert.Contains("publicação", Assert.Throws<DomainException>(() =>
            Ba103(StatusImovel.Rascunho).GarantirNovoStatusValido(StatusImovel.Ativo)).Message);
        Assert.Throws<DomainException>(() => Ba103().GarantirNovoStatusValido(StatusImovel.Rascunho));
    }

    [Fact]
    public void Publicacao_so_de_rascunho_ou_pausado()
    {
        Assert.Equal(CodigoErro.Conflito, Assert.Throws<DomainException>(() => Ba103().GarantirQuePodeSerPublicado()).Codigo);
        Assert.Throws<DomainException>(() => Ba103(StatusImovel.Vendido).GarantirQuePodeSerPublicado());
        Ba103(StatusImovel.Rascunho).GarantirQuePodeSerPublicado();
    }

    // --- Ação pendente -------------------------------------------------------------------------

    private static AcaoPendente AcaoDeStatus(Imovel imovel) =>
        AcaoPendente.MudarStatus(Op1, imovel, StatusImovel.Vendido, "resumo", Agora.AddMinutes(-5));

    [Fact]
    public void Aceita_acao_pendente_do_operador_dentro_do_prazo()
    {
        var acao = AcaoDeStatus(Ba103());
        Assert.Same(acao, AcaoPendente.Verificar(acao, Op1, Agora));
    }

    [Fact]
    public void Recusa_outro_operador_acao_expirada_confirmada_ou_cancelada()
    {
        var acao = AcaoDeStatus(Ba103());
        Assert.Equal(CodigoErro.NaoEncontrado, Assert.Throws<DomainException>(() => AcaoPendente.Verificar(acao, Op2, Agora)).Codigo);
        Assert.Equal(CodigoErro.Expirada, Assert.Throws<DomainException>(() => AcaoPendente.Verificar(acao, Op1, Agora.AddMinutes(11))).Codigo);
        Assert.Equal(CodigoErro.NaoEncontrado, Assert.Throws<DomainException>(() => AcaoPendente.Verificar(null, Op1, Agora)).Codigo);

        acao.Cancelar();
        Assert.Equal(CodigoErro.Conflito, Assert.Throws<DomainException>(() => AcaoPendente.Verificar(acao, Op1, Agora)).Codigo);
    }

    [Fact]
    public void Executa_a_acao_e_nao_deixa_confirmar_de_novo()
    {
        var imovel = Ba103();
        var acao = AcaoDeStatus(imovel);

        acao.Executar(imovel, new DadosInternosImovel(imovel.Id, Agora), "Centro", "Bruna", Agora);

        Assert.Equal(StatusImovel.Vendido, imovel.Status);
        Assert.Equal(CodigoErro.Conflito, Assert.Throws<DomainException>(() => AcaoPendente.Verificar(acao, Op1, Agora)).Codigo);
    }

    [Fact]
    public void Nao_executa_se_o_imovel_mudou_depois_da_proposta()
    {
        var imovel = Ba103();
        var acao = AcaoDeStatus(imovel);
        imovel.DefinirDestaque(true, Agora.AddMinutes(-1)); // editado no painel

        var erro = Assert.Throws<DomainException>(() =>
            acao.Executar(imovel, new DadosInternosImovel(imovel.Id, Agora), "Centro", "Bruna", Agora));
        Assert.Equal(CodigoErro.Conflito, erro.Codigo);
        Assert.Equal(StatusImovel.Ativo, imovel.Status);
    }

    [Fact]
    public void Alteracao_confirmada_grava_os_dados_internos_com_o_autor()
    {
        var imovel = Ba103();
        var internos = new DadosInternosImovel(imovel.Id, Agora);
        var atual = imovel.ParaDados(internos);
        var proposto = new CamposDoImovel { Preco = 349_000, Endereco = "Rua B, 200" }.AplicarEm(atual);
        var acao = AcaoPendente.AlterarCampos(Op1, imovel, TipoAcao.Alterar, proposto, Diferencas.Entre(atual, proposto), "r", Agora);

        acao.Executar(imovel, internos, "Centro", "Bruna", Agora);

        Assert.Equal(349_000, imovel.Preco);
        Assert.Equal("Rua B, 200", internos.Endereco);
        Assert.Equal("Bruna", internos.AtualizadoPor);
    }

    [Fact]
    public void Proposta_sem_mudanca_e_destaque_repetido_sao_recusados()
    {
        var imovel = Ba103();
        var atual = imovel.ParaDados(null);
        Assert.Throws<DomainException>(() => AcaoPendente.AlterarCampos(Op1, imovel, TipoAcao.Alterar, atual, [], "r", Agora));
        Assert.Contains("já não está", Assert.Throws<DomainException>(() => AcaoPendente.MudarDestaque(Op1, imovel, false, "r", Agora)).Message);
    }

    [Fact]
    public void Desfazer_so_a_propria_acao_confirmada_ate_30_minutos()
    {
        var antes = Ba103().ParaDados(null);
        var depois = antes with { Preco = 349_000 };
        RegistroDeAuditoria Registro(string operacao, DateTimeOffset quando) =>
            RegistroDeAuditoria.Sucedido(Op1, operacao, Guid.CreateVersion7(), quando, antes, depois);
        var confirmado = Registro(RegistroDeAuditoria.ConfirmarAcao, Agora.AddMinutes(-15));

        Assert.Same(confirmado, RegistroDeAuditoria.VerificarDesfazer(confirmado, Op1, Agora));
        Assert.Throws<DomainException>(() => RegistroDeAuditoria.VerificarDesfazer(confirmado, Op2, Agora));
        Assert.Contains("30 minutos", Assert.Throws<DomainException>(() =>
            RegistroDeAuditoria.VerificarDesfazer(Registro(RegistroDeAuditoria.ConfirmarAcao, Agora.AddMinutes(-40)), Op1, Agora)).Message);
        Assert.Throws<DomainException>(() =>
            RegistroDeAuditoria.VerificarDesfazer(Registro(RegistroDeAuditoria.CriarRascunho, Agora), Op1, Agora));
    }

    [Fact]
    public void Desfazer_volta_so_os_campos_que_a_alteracao_mudou_inclusive_para_vazio()
    {
        var antes = Ba103().ParaDados(null) with { Condominio = null };
        var depois = antes with { Preco = 349_000, Condominio = 300 };
        var registro = RegistroDeAuditoria.Sucedido(Op1, RegistroDeAuditoria.ConfirmarAcao, Guid.CreateVersion7(), Agora, antes, depois);
        var atual = depois with { Titulo = "Título mudado no painel" };

        var proposto = registro.EstadoParaDesfazer(atual);

        Assert.Equal(365_000, proposto.Preco);
        Assert.Null(proposto.Condominio);
        Assert.Equal("Título mudado no painel", proposto.Titulo);
        Assert.Equal(["preco", "condominio"], registro.CamposAlterados);
    }

    // --- Operador ------------------------------------------------------------------------------

    [Fact]
    public void Operador_desativado_nao_e_autorizado()
    {
        var operador = new Operador("(49) 99000-0000", "Bruna", Agora);
        Assert.Same(operador, Operador.GarantirAutorizado(operador));

        operador.Desativar();
        Assert.Equal(CodigoErro.NaoAutorizado, Assert.Throws<DomainException>(() => Operador.GarantirAutorizado(operador)).Codigo);
        Assert.Throws<DomainException>(() => Operador.GarantirAutorizado(null));
    }

    [Fact]
    public void Reserva_vale_para_um_processamento_por_vez()
    {
        var operador = new Operador("5549990000000", "Bruna", Agora);

        Assert.True(operador.Reservar("a", Agora.AddMinutes(1), Agora));
        Assert.False(operador.Reservar("b", Agora.AddMinutes(1), Agora));
        Assert.True(operador.Reservar("a", Agora.AddMinutes(2), Agora)); // renovação
        Assert.True(operador.Reservar("b", Agora.AddMinutes(4), Agora.AddMinutes(3))); // reserva de "a" venceu

        operador.Liberar("a"); // token antigo não libera a reserva de outro
        Assert.Equal("b", operador.ProcessandoToken);
    }
}
