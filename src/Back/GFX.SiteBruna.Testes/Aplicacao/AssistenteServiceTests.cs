using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Aplicacao;

public class AssistenteServiceTests
{
    private const string FichaNova = """
        FICHA DO IMÓVEL
        Finalidade: venda
        Tipo: casa
        Título: Casa 3 quartos com quintal
        Bairro: Centro
        Preço: 420 mil
        Quartos: 3
        Proprietário (não aparece no site): João Souza
        Descrição:
        Casa ampla e iluminada.
        """;

    private readonly Mundo _m = new();
    private readonly Imovel _ba103;

    public AssistenteServiceTests()
    {
        _ba103 = _m.CriarImovel(Mundo.Venda(), quantasFotos: 2, bairro: "Centro");
    }

    private (string Telefone, string Texto, IReadOnlyList<BotaoDoWhatsapp>? Botoes) Ultima => _m.Enviadas[^1];
    private Imovel? Rascunho => _m.ImoveisGravados.SingleOrDefault(i => i.Titulo == "Casa 3 quartos com quintal");

    // --- Ficha -----------------------------------------------------------------------------------

    [Fact]
    public async Task Cria_o_rascunho_com_bairro_e_proprietario_e_pede_as_fotos()
    {
        await _m.Texto(FichaNova);

        var r = Rascunho!;
        Assert.Equal(StatusImovel.Rascunho, r.Status);
        Assert.Equal(OrigemImovel.Whatsapp, r.Origem);
        Assert.Equal(420_000, r.Preco);
        Assert.Equal(3, r.Quartos);
        Assert.Equal(_m.Bairro("Centro").Id, r.BairroId);
        Assert.Equal("Casa ampla e iluminada.", r.Descricao);
        Assert.Equal("João Souza", _m.InternosDe(r.Id)!.ProprietarioNome);
        Assert.Contains($"Rascunho *{r.Codigo}* criado", Ultima.Texto);
        Assert.All(_m.FilaGravada, x => Assert.Equal(StatusMensagem.Processada, x.Status));
    }

    [Fact]
    public async Task Responde_todos_os_problemas_de_uma_vez_e_nao_cria_nada()
    {
        var antes = _m.ImoveisGravados.Count;
        await _m.Texto("FICHA DO IMÓVEL\nFinalidade: venda\nTipo: casa\nTítulo: Casa\nBairro: Centrinho\nPreço: a combinar");

        Assert.Equal(antes, _m.ImoveisGravados.Count);
        Assert.Contains("Preço: não entendi", Ultima.Texto);
        Assert.Contains("Bairro: não encontrei \"Centrinho\"", Ultima.Texto);
    }

    [Fact]
    public async Task Bairro_parecido_vira_sugestao()
    {
        await _m.Texto("FICHA DO IMÓVEL\nFinalidade: venda\nTipo: casa\nTítulo: Casa\nBairro: Santa");
        Assert.Contains("Bairro: não encontrei \"Santa\". Seria Santa Gema?", Ultima.Texto);
    }

    [Fact]
    public async Task Com_codigo_de_rascunho_atualiza_e_de_imovel_publicado_pede_confirmacao()
    {
        await _m.Texto(FichaNova);
        var r = Rascunho!;
        await _m.Texto($"FICHA DO IMÓVEL\nCódigo: {r.Codigo}\nPreço: 399 mil");
        Assert.Equal(399_000, _m.Imovel(r.Codigo).Preco);
        Assert.Contains("atualizado", Ultima.Texto);
        Assert.Equal("João Souza", _m.InternosDe(r.Id)!.ProprietarioNome);

        await _m.Texto($"FICHA DO IMÓVEL\nCódigo: {_ba103.Codigo}\nPreço: 349 mil");
        Assert.Equal(365_000, _m.Imovel(_ba103.Codigo).Preco); // nada muda sem o botão
        Assert.Equal(["Confirmar", "Cancelar"], Ultima.Botoes!.Select(b => b.Titulo));

        await _m.Botao(Ultima.Botoes![0].Id);
        Assert.Equal(349_000, _m.Imovel(_ba103.Codigo).Preco);
        Assert.Contains("O site já mostra as alterações", Ultima.Texto);
    }

    [Fact]
    public async Task Ficha_nao_muda_a_finalidade_de_um_imovel()
    {
        await _m.Texto($"FICHA DO IMÓVEL\nCódigo: {_ba103.Codigo}\nFinalidade: aluguel\nAluguel: 1.650");
        Assert.Contains($"o {_ba103.Codigo} é de venda", Ultima.Texto);
        Assert.Empty(_m.AcoesGravadas);
    }

    [Fact]
    public async Task Ficha_envia_a_instrucao_e_os_dois_modelos()
    {
        await _m.Texto("ficha");
        Assert.Equal(3, _m.Enviadas.Count);
        Assert.Contains("Copie a ficha", _m.Enviadas[0].Texto);
        Assert.StartsWith("FICHA DO IMÓVEL\nFinalidade: venda", _m.Enviadas[1].Texto);
        Assert.StartsWith("FICHA DO IMÓVEL\nFinalidade: aluguel", _m.Enviadas[2].Texto);
    }

    // --- Fotos e publicação ----------------------------------------------------------------------

    [Fact]
    public async Task Agrupa_as_fotos_numa_resposta_so_e_publica_pelo_botao()
    {
        await _m.Texto(FichaNova);
        var r = Rascunho!;
        _m.Enviadas.Clear();

        await _m.Enviar(FotoMensagem(), FotoMensagem(), FotoMensagem());

        Assert.Equal(3, _m.FotosDe(r.Id).Count);
        Assert.Single(_m.FotosDe(r.Id), f => f.Capa);
        Assert.Single(_m.Enviadas); // uma resposta para o lote
        Assert.Contains("Fotos recebidas", Ultima.Texto);
        Assert.Contains("• 3 foto(s)", Ultima.Texto);
        Assert.Equal(["Publicar", "Agora não"], Ultima.Botoes!.Select(b => b.Titulo));
        Assert.Equal(StatusImovel.Rascunho, _m.Imovel(r.Codigo).Status);

        await _m.Botao(Ultima.Botoes![0].Id);
        Assert.Equal(StatusImovel.Ativo, _m.Imovel(r.Codigo).Status);
        Assert.Contains("já está no site", Ultima.Texto);
        Assert.Contains("https://site.teste/imovel/", Ultima.Texto);
    }

    [Fact]
    public async Task Foto_que_chega_durante_a_espera_entra_no_mesmo_lote()
    {
        await _m.Texto(FichaNova);
        var r = Rascunho!;
        _m.Enviadas.Clear();
        _m.AoEsperar = async () => await _m.Assistente.Receber([FotoMensagem()]);

        await _m.Enviar(FotoMensagem());

        Assert.Equal(2, _m.FotosDe(r.Id).Count);
        Assert.Single(_m.Enviadas);
    }

    [Fact]
    public async Task Agora_nao_mantem_o_rascunho_e_explica_como_publicar_depois()
    {
        await _m.Enviar(_m.Mensagem(new ConteudoMensagem.Texto(FichaNova)), FotoMensagem());
        var r = Rascunho!;

        await _m.Botao(Ultima.Botoes![1].Id);

        Assert.Equal(StatusImovel.Rascunho, _m.Imovel(r.Codigo).Status);
        Assert.Contains($"publicar {r.Codigo}", Ultima.Texto);
    }

    [Fact]
    public async Task Sim_confirma_quando_ha_uma_unica_confirmacao_aberta()
    {
        await _m.Enviar(_m.Mensagem(new ConteudoMensagem.Texto(FichaNova)), FotoMensagem());
        var r = Rascunho!;

        await _m.Texto("sim");
        Assert.Equal(StatusImovel.Ativo, _m.Imovel(r.Codigo).Status);

        await _m.Texto("sim");
        Assert.Equal("Não há nada esperando confirmação agora.", Ultima.Texto);
    }

    [Fact]
    public async Task Foto_sem_imovel_escolhido_avisa_uma_vez_por_lote()
    {
        await _m.Enviar(FotoMensagem(), FotoMensagem());
        Assert.Single(_m.Enviadas);
        Assert.Contains("De qual imóvel são as fotos?", Ultima.Texto);
    }

    [Fact]
    public async Task Foto_para_imovel_publicado_nao_entra()
    {
        await _m.Foto(_ba103.Codigo);
        Assert.Equal(2, _m.FotosDe(_ba103.Id).Count);
        Assert.Contains("já está no site", Ultima.Texto);
    }

    // --- Segurança e fila ------------------------------------------------------------------------

    [Fact]
    public async Task Telefone_nao_autorizado_registra_e_nao_responde()
    {
        await _m.Texto(FichaNova, "5549988887777");
        await _m.Texto(FichaNova, Mundo.TelefoneDesativado);

        Assert.Empty(_m.Enviadas);
        Assert.All(_m.FilaGravada, x => Assert.Equal(StatusMensagem.Ignorada, x.Status));
        Assert.Null(_m.FilaGravada[0].OperadorId);
        Assert.Null(Rascunho);
    }

    [Fact]
    public async Task Reenvio_do_mesmo_wamid_pelo_webhook_e_ignorado()
    {
        var mensagem = _m.Mensagem(new ConteudoMensagem.Texto(FichaNova));
        await _m.Enviar(mensagem);
        await _m.Enviar(mensagem);
        Assert.Single(_m.ImoveisGravados, i => i.Titulo == "Casa 3 quartos com quintal");
    }

    [Fact]
    public async Task So_um_processamento_por_operador_e_libera_ao_terminar()
    {
        await _m.Assistente.Receber([_m.Mensagem(new ConteudoMensagem.Texto("ajuda"))]);
        var bruna = _m.Operador(Mundo.TelefoneBruna);
        await _m.Operadores.Reservar(bruna.Id, "outro", _m.Agora.AddMinutes(1), _m.Agora);

        await _m.Assistente.ProcessarFila(Mundo.TelefoneBruna);
        Assert.Empty(_m.Enviadas); // outro processamento está com o operador

        await _m.Operadores.Liberar(bruna.Id, "outro");
        await _m.Assistente.ProcessarFila(Mundo.TelefoneBruna);
        Assert.Single(_m.Enviadas);
        Assert.Null(_m.Operador(Mundo.TelefoneBruna).ProcessandoToken);
    }

    [Fact]
    public async Task Texto_livre_nao_altera_nada_e_responde_o_menu()
    {
        await _m.Texto($"muda o preço do {_ba103.Codigo} para 1 real");
        Assert.Equal(365_000, _m.Imovel(_ba103.Codigo).Preco);
        Assert.Contains("Mande *ficha*", Ultima.Texto);
    }

    [Fact]
    public async Task Erro_de_negocio_vira_resposta_e_a_fila_segue()
    {
        await _m.Texto(FichaNova);
        var quebrada = _m.Mensagem(new ConteudoMensagem.Imagem("nao-existe", null, null));

        await _m.Enviar(quebrada, _m.Mensagem(new ConteudoMensagem.Texto("ajuda")));

        Assert.Contains("Não consegui baixar a foto", _m.Enviadas[^2].Texto);
        Assert.Contains("Mande *ficha*", Ultima.Texto);
    }

    [Fact]
    public async Task Erro_inesperado_vira_mensagem_amigavel_e_fica_registrado()
    {
        await _m.Texto(FichaNova);
        _m.FalharProcessamento = true;

        await _m.Foto();

        Assert.Contains("Não consegui concluir agora", _m.Enviadas.First(e => e.Texto.Contains("Não consegui")).Texto);
        Assert.Contains(_m.FilaGravada, x => x.Status == StatusMensagem.Erro);
        Assert.Empty(_m.FotosDe(Rascunho!.Id));
    }

    [Fact]
    public async Task Codigo_inexistente_sugere_os_recentes()
    {
        await _m.Texto("BA999");
        Assert.Contains("Não existe imóvel BA999.", Ultima.Texto);
        Assert.Contains($"Veja se é um destes: {_ba103.Codigo}", Ultima.Texto);
    }

    private MensagemRecebida FotoMensagem()
    {
        var midia = $"midia-{Guid.NewGuid():N}";
        _m.Midias[midia] = Mundo.Jpeg();
        return _m.Mensagem(new ConteudoMensagem.Imagem(midia, "image/jpeg", null));
    }
}
