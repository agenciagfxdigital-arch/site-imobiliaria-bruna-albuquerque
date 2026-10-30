using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class AssistenteTests
{
    private static readonly Guid Acao = Guid.Parse("3f2b8c1e-0000-4000-8000-000000000001");

    private static Comando Texto(string t) => Assistente.Interpretar(new ConteudoMensagem.Texto(t));

    [Fact]
    public void Entende_os_comandos_de_texto()
    {
        Assert.Equal(new Comando.PedirFicha(), Texto("Ficha"));
        Assert.Equal(new Comando.Selecionar("BA107"), Texto("ba107"));
        Assert.Equal(new Comando.Publicar("BA107"), Texto("Publicar BA107"));
        Assert.Equal(new Comando.Publicar(null), Texto("publicar"));
        Assert.Equal(new Comando.Sim(), Texto("Sim!"));
        Assert.Equal(new Comando.Nao(), Texto("não"));
        Assert.Equal(new Comando.Ajuda(), Texto("bom dia"));
    }

    [Fact]
    public void Reconhece_a_ficha_preenchida()
    {
        const string ficha = "FICHA DO IMÓVEL\nFinalidade: venda\nTipo: casa\nTítulo: Casa";
        Assert.Equal(new Comando.FichaEnviada(ficha), Texto(ficha));
    }

    [Fact]
    public void Botoes_so_aceitam_os_ids_gerados_pelo_sistema()
    {
        var botoes = Assistente.BotoesDePublicacao(Acao);

        Assert.Equal(new Comando.Confirmar(Acao), Assistente.Interpretar(new ConteudoMensagem.Botao(botoes[0].Id, null)));
        Assert.Equal(new Comando.Cancelar(Acao), Assistente.Interpretar(new ConteudoMensagem.Botao(botoes[1].Id, null)));
        Assert.Equal(new Comando.Ajuda(), Assistente.Interpretar(new ConteudoMensagem.Botao("ok:qualquer-coisa", null)));
        Assert.Equal(new Comando.Ajuda(), Assistente.Interpretar(new ConteudoMensagem.Botao($"apagar:{Acao}", null)));
    }

    [Fact]
    public void Foto_com_o_codigo_na_legenda_vai_para_aquele_imovel()
    {
        Assert.Equal(new Comando.Foto("m1", "BA103"), Assistente.Interpretar(new ConteudoMensagem.Imagem("m1", "image/jpeg", " ba103 ")));
        Assert.Equal(new Comando.Foto("m2", null), Assistente.Interpretar(new ConteudoMensagem.Imagem("m2", null, "sala")));
    }

    [Fact]
    public void Os_titulos_dos_botoes_cabem_no_limite_do_whatsapp()
    {
        foreach (var b in Assistente.BotoesDePublicacao(Acao).Concat(Assistente.BotoesDeConfirmacao(Acao)))
            Assert.True(b.Titulo.Length <= BotaoDoWhatsapp.TamanhoMaximoTitulo, b.Titulo);
    }

    [Fact]
    public void Problemas_da_ficha_viram_lista()
    {
        Assert.Equal(
            "Preciso de alguns ajustes na ficha:\n• Tipo: use apartamento, casa, terreno ou sala comercial.\n\nCorrija e envie a ficha de novo.",
            TextosDoAssistente.ProblemasDaFicha(["Tipo: use apartamento, casa, terreno ou sala comercial."]));
    }
}
