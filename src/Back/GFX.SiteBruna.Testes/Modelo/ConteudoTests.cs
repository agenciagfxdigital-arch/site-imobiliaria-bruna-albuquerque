using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class ConteudoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    // --- Páginas -----------------------------------------------------------------------------------

    [Fact]
    public void Sem_nada_salvo_as_paginas_usam_os_textos_revisados_pela_Bruna()
    {
        var paginas = new Paginas();
        Assert.Equal("Seu próximo imóvel começa com uma boa escolha.", paginas.Home.HeroTitulo);
        Assert.Equal(["Compra", "Venda", "Locação", "Imóvel na planta", "Minha Casa Minha Vida"], paginas.Home.Servicos);
        Assert.Equal(4, paginas.Home.Passos.Count);
        Assert.Contains("7 anos no mercado imobiliário", paginas.Sobre.Subtitulo);
        Assert.Equal(6, paginas.Mcmv.Entenda.Count);
    }

    [Fact]
    public void Os_textos_padrao_passam_na_propria_validacao()
    {
        ConteudoHome.Padrao.Validar();
        ConteudoSobre.Padrao.Validar();
        ConteudoMcmv.Padrao.Validar();
    }

    [Fact]
    public void Recusa_texto_grande_demais_com_o_nome_que_o_painel_mostra()
    {
        var paginas = new Paginas();
        var erro = Assert.Throws<DomainException>(() =>
            paginas.AtualizarHome(ConteudoHome.Padrao with { HeroTitulo = new string('x', 201) }, Agora));
        Assert.Equal("O campo Título principal pode ter até 200 letras.", erro.Message);
        Assert.Equal(ConteudoHome.Padrao.HeroTitulo, paginas.Home.HeroTitulo);
    }

    [Fact]
    public void Recusa_passo_sem_titulo_e_lista_longa_demais()
    {
        var paginas = new Paginas();
        Assert.Throws<DomainException>(() =>
            paginas.AtualizarSobre(ConteudoSobre.Padrao with { Passos = [new Passo("", "texto")] }, Agora));
        Assert.Throws<DomainException>(() =>
            paginas.AtualizarMcmv(ConteudoMcmv.Padrao with { Documentos = [.. Enumerable.Repeat("doc", 31)] }, Agora));
    }

    // --- Configurações -----------------------------------------------------------------------------

    private static DadosDasConfiguracoes Config() => new()
    {
        Whatsapp = "+55 (49) 99930-4262",
        MensagemPadrao = "Olá, Bruna! Vim pelo site.",
        Creci = "CRECI/SC 60723",
        Cidade = "Videira/SC",
        DiasSeloVendido = 30,
    };

    [Fact]
    public void Configuracoes_padrao_tem_o_contato_da_Bruna()
    {
        var c = new Configuracoes();
        Assert.Equal("5549999304262", c.Whatsapp);
        Assert.Equal("CRECI/SC 60723", c.Creci);
        Assert.Equal(30, c.DiasSeloVendido);
    }

    [Fact]
    public void Configuracoes_limpam_o_whatsapp_e_validam_os_ids()
    {
        var c = new Configuracoes();
        c.Atualizar(Config() with { Ga4Id = "g-abc123", MetaPixelId = "123456789" }, Agora);
        Assert.Equal("5549999304262", c.Whatsapp);
        Assert.Equal("G-ABC123", c.Ga4Id);

        Assert.Throws<DomainException>(() => c.Atualizar(Config() with { Whatsapp = "9999-4262" }, Agora));
        Assert.Throws<DomainException>(() => c.Atualizar(Config() with { Ga4Id = "UA-123" }, Agora));
        Assert.Throws<DomainException>(() => c.Atualizar(Config() with { MetaPixelId = "abc" }, Agora));
        Assert.Throws<DomainException>(() => c.Atualizar(Config() with { Email = "bruna@" }, Agora));
        Assert.Throws<DomainException>(() => c.Atualizar(Config() with { DiasSeloVendido = 400 }, Agora));
    }

    // --- FAQ e depoimentos -------------------------------------------------------------------------

    [Fact]
    public void Faq_valida_e_reordena_so_com_a_lista_completa()
    {
        var a = new Faq("Sou autônomo. Consigo financiar?", "Na maioria dos casos, sim.", 1, Agora);
        var b = new Faq("Posso usar o FGTS?", "Pode, conforme as regras.", 2, Agora);

        Faq.Reordenar([a, b], [b.Id, a.Id]);
        Assert.Equal((1, 2), (b.Ordem, a.Ordem));

        Assert.Throws<DomainException>(() => Faq.Reordenar([a, b], [a.Id]));
        Assert.Throws<DomainException>(() => Faq.Reordenar([a, b], [a.Id, a.Id]));
        Assert.Throws<DomainException>(() => new Faq("Oi", "Resposta", 1, Agora));
    }

    [Fact]
    public void Depoimento_sem_foto_fica_nulo_e_texto_curto_e_recusado()
    {
        var d = new Depoimento(" Carlos ", "Muito obrigado por não desistir.", "  ", true, 1, Agora);
        Assert.Equal("Carlos", d.Nome);
        Assert.Null(d.FotoUrl);
        Assert.Throws<DomainException>(() => new Depoimento("Carlos", "Oi", null, true, 1, Agora));
    }
}
