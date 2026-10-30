using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Conteudo;
using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;
using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.Extensions.DependencyInjection;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>Serviços de conteúdo, eventos e usuários com o banco de verdade (cada passo num escopo, como na Api).</summary>
public class ConteudoTests(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    private const string Robo = "WhatsApp/2.23.20.0";
    private const string Celular = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)";

    private async Task<T> Com<TServico, T>(Func<TServico, Task<ApiResponse<T>>> chamada) where TServico : notnull
    {
        await using var escopo = banco.Escopo();
        var resposta = await chamada(escopo.ServiceProvider.GetRequiredService<TServico>());
        Assert.True(resposta.Sucesso, string.Join(" ", resposta.Mensagens));
        return resposta.Dados!;
    }

    private async Task<ApiResponse<T>> Tentar<TServico, T>(Func<TServico, Task<ApiResponse<T>>> chamada) where TServico : notnull
    {
        await using var escopo = banco.Escopo();
        return await chamada(escopo.ServiceProvider.GetRequiredService<TServico>());
    }

    [Fact]
    public async Task Configuracoes_comecam_com_o_padrao_e_gravam_depois_de_salvas()
    {
        var padrao = await Com<ConteudoService, ConfiguracoesResponse>(s => s.Configuracoes());
        Assert.Equal("CRECI/SC 60723", padrao.Creci);

        await Com<ConteudoService, ConfiguracoesResponse>(s => s.SalvarConfiguracoes(new DadosDasConfiguracoes
        {
            Whatsapp = "55 49 99930-4262", MensagemPadrao = "Olá, Bruna!", Creci = "CRECI/SC 60723", Cidade = "Videira/SC",
            Instagram = "@bruna", DiasSeloVendido = 15,
        }));

        var lidas = await Com<ConteudoService, ConfiguracoesResponse>(s => s.Configuracoes());
        Assert.Equal(("@bruna", 15), (lidas.Instagram, lidas.DiasSeloVendido));
    }

    [Fact]
    public async Task Paginas_gravam_em_jsonb_e_mantem_as_outras_com_o_padrao()
    {
        var home = ConteudoHome.Padrao with
        {
            HeroTitulo = "Título novo",
            Servicos = ["Compra", "Venda"],
            Passos = [new Passo("Conversa", "Você me conta o que procura.")],
        };
        await Com<ConteudoService, bool>(s => s.SalvarHome(home));

        var paginas = await Com<ConteudoService, PaginasResponse>(s => s.Paginas());
        Assert.Equal("Título novo", paginas.Home.HeroTitulo);
        Assert.Equal(["Compra", "Venda"], paginas.Home.Servicos);
        Assert.Equal(new Passo("Conversa", "Você me conta o que procura."), Assert.Single(paginas.Home.Passos));
        Assert.Equal(ConteudoSobre.Padrao.Historia, paginas.Sobre.Historia);
    }

    [Fact]
    public async Task Faq_salva_reordena_e_exclui()
    {
        var a = await Com<FaqService, FaqResponse>(s => s.Salvar(new(null, "Sou autônomo. Consigo financiar?", "Na maioria dos casos, sim.", 1)));
        var b = await Com<FaqService, FaqResponse>(s => s.Salvar(new(null, "Posso usar o FGTS?", "Pode, conforme as regras.", 2)));

        await Com<FaqService, bool>(s => s.Reordenar([b.Id, a.Id]));
        Assert.Equal([b.Id, a.Id], (await Com<FaqService, IReadOnlyList<FaqResponse>>(s => s.Listar())).Select(f => f.Id));

        await Com<FaqService, bool>(s => s.Excluir(a.Id));
        Assert.Equal(TipoFalha.Validacao, (await Tentar<FaqService, bool>(s => s.Reordenar([a.Id, b.Id]))).Falha);
    }

    [Fact]
    public async Task Depoimentos_da_home_e_todos()
    {
        await Com<DepoimentoService, DepoimentoResponse>(s => s.Salvar(new(null, "Carlos", "Muito obrigado por não desistir.", null, true, 1)));
        await Com<DepoimentoService, DepoimentoResponse>(s => s.Salvar(new(null, "Interno", "Só na página Sobre.", null, false, 2)));

        Assert.All(await Com<DepoimentoService, IReadOnlyList<DepoimentoResponse>>(s => s.Listar(somenteDaHome: true)),
            d => Assert.True(d.ExibirNaHome));
        Assert.True((await Com<DepoimentoService, IReadOnlyList<DepoimentoResponse>>(s => s.Listar(false))).Count >= 2);
    }

    [Fact]
    public async Task Bairro_repetido_e_bairro_em_uso_sao_recusados()
    {
        var bairro = await Com<BairroService, BairroResponse>(s => s.Salvar(new(null, "Rio das Pedras")));
        var repetido = await Tentar<BairroService, BairroResponse>(s => s.Salvar(new(null, "rio das pedras")));
        Assert.Equal(["Esse bairro já está cadastrado."], repetido.Mensagens);

        await using (var escopo = banco.Escopo())
        {
            var imoveis = escopo.ServiceProvider.GetRequiredService<IImovelRepository>();
            imoveis.Add(new Imovel(new DadosDoImovel
            {
                Finalidade = Finalidade.Venda, Tipo = TipoImovel.Casa, Titulo = "Casa em Rio das Pedras", BairroId = bairro.Id,
            }, await imoveis.ProximoCodigo(), bairro.Nome, OrigemImovel.Painel, DateTimeOffset.UtcNow));
            await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().Commit();
        }
        var emUso = await Tentar<BairroService, bool>(s => s.Excluir(bairro.Id));
        Assert.Equal(["Esse bairro está em 1 imóvel(is). Troque o bairro deles antes de excluir."], emUso.Mensagens);
    }

    [Fact]
    public async Task Eventos_ignoram_robo_visualizacao_repetida_e_imovel_inexistente()
    {
        var visitante = $"visitante-{Guid.NewGuid():N}"[..30];
        Guid imovelId;
        await using (var escopo = banco.Escopo())
        {
            var imoveis = escopo.ServiceProvider.GetRequiredService<IImovelRepository>();
            var imovel = new Imovel(new DadosDoImovel { Finalidade = Finalidade.Venda, Tipo = TipoImovel.Casa, Titulo = "Casa vista" },
                await imoveis.ProximoCodigo(), null, OrigemImovel.Painel, DateTimeOffset.UtcNow);
            imoveis.Add(imovel);
            await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().Commit();
            imovelId = imovel.Id;
        }
        var visualizacao = new DadosDoEvento { Tipo = TipoEvento.PropertyView, ImovelId = imovelId, VisitanteId = visitante };

        Assert.True(await Com<EventoService, bool>(s => s.Registrar(visualizacao, Celular)));
        Assert.False(await Com<EventoService, bool>(s => s.Registrar(visualizacao, Celular)));
        Assert.False(await Com<EventoService, bool>(s => s.Registrar(visualizacao with { VisitanteId = "outro-visitante" }, Robo)));
        Assert.False(await Com<EventoService, bool>(s => s.Registrar(visualizacao with { ImovelId = Guid.NewGuid() }, Celular)));
        Assert.True(await Com<EventoService, bool>(s => s.Registrar(new DadosDoEvento
        {
            Tipo = TipoEvento.WhatsappClick, VisitanteId = visitante, Detalhes = new Dictionary<string, string?> { ["local"] = "home_hero" },
        }, Celular)));
    }

    [Fact]
    public async Task Usuario_entra_com_a_senha_certa_e_email_repetido_e_recusado()
    {
        const string senha = "Videira#2026casa";
        var email = $"bruna.{Guid.NewGuid():N}@exemplo.com";
        var criado = await Com<UsuarioService, UsuarioResponse>(s => s.Criar(new(email.ToUpperInvariant(), "Bruna", senha)));
        Assert.Equal(email, criado.Email);

        var sessao = await Com<UsuarioService, SessaoResponse>(s => s.Entrar(new(email, senha)));
        Assert.NotNull(sessao.Usuario.UltimoAcessoEm);
        Assert.False(string.IsNullOrEmpty(sessao.Token));

        var errada = await Tentar<UsuarioService, SessaoResponse>(s => s.Entrar(new(email, "Outra#Senha2026")));
        var inexistente = await Tentar<UsuarioService, SessaoResponse>(s => s.Entrar(new("ninguem@exemplo.com", senha)));
        var invalido = await Tentar<UsuarioService, SessaoResponse>(s => s.Entrar(new("sem-arroba", senha)));
        Assert.All([errada, inexistente, invalido], r =>
        {
            Assert.Equal(TipoFalha.NaoAutorizado, r.Falha);
            Assert.Equal(["E-mail ou senha incorretos."], r.Mensagens);
        });

        Assert.Equal(TipoFalha.Conflito, (await Tentar<UsuarioService, UsuarioResponse>(s => s.Criar(new(email, "Outra", senha)))).Falha);
    }
}
