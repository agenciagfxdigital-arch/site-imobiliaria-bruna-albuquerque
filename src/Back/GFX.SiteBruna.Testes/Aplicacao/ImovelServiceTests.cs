using GFX.SiteBruna.Aplicacao.DTOs;
using GFX.SiteBruna.Aplicacao.DTOs.Imoveis;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Aplicacao;

public class ImovelServiceTests
{
    private readonly Mundo _m = new();
    private readonly Imovel _ba103;

    public ImovelServiceTests()
    {
        _ba103 = _m.CriarImovel(Mundo.Venda(), quantasFotos: 2, bairro: "Centro");
    }

    private static T Ok<T>(ApiResponse<T> r)
    {
        Assert.True(r.Sucesso, string.Join(" ", r.Mensagens));
        return r.Dados!;
    }

    [Fact]
    public async Task Cria_imovel_novo_com_slug_com_o_bairro_e_avisa_o_site()
    {
        var dados = Mundo.Venda("Casa 2 quartos com quintal") with { BairroId = _m.Bairro("Santa Gema").Id };
        var salvo = Ok(await _m.ImovelService.Salvar(new(null, dados)));

        var imovel = _m.Imovel(salvo.Codigo);
        Assert.Equal("casa-2-quartos-com-quintal-santa-gema", imovel.Slug);
        Assert.StartsWith("BA", salvo.Codigo);
        Assert.Equal(1, _m.Invalidacoes);
    }

    [Fact]
    public async Task Atualiza_mantendo_o_codigo_e_recusa_codigo_de_outro_imovel()
    {
        var outro = _m.CriarImovel(Mundo.Venda("Outro"));
        Ok(await _m.ImovelService.Salvar(new(_ba103.Id, Mundo.Venda("Apartamento reformado"))));
        Assert.Equal("Apartamento reformado", _m.Imovel(_ba103.Codigo).Titulo);

        var r = await _m.ImovelService.Salvar(new(_ba103.Id, Mundo.Venda() with { Codigo = outro.Codigo }));
        Assert.False(r.Sucesso);
        Assert.Equal(TipoFalha.Conflito, r.Falha);
        Assert.Equal(["Já existe um imóvel com esse código."], r.Mensagens);
    }

    [Fact]
    public async Task Dados_internos_ficam_a_parte_mantidos_quando_ausentes_e_apagados_quando_vazios()
    {
        Ok(await _m.ImovelService.Salvar(new(_ba103.Id, Mundo.Venda() with { ProprietarioNome = "Maria", Endereco = "Rua A, 100" })));
        Ok(await _m.ImovelService.Salvar(new(_ba103.Id, Mundo.Venda())));
        Assert.Equal("Maria", _m.InternosDe(_ba103.Id)!.ProprietarioNome);

        Ok(await _m.ImovelService.Salvar(new(_ba103.Id, Mundo.Venda() with { ProprietarioNome = "", Endereco = "" })));
        Assert.Null(_m.InternosDe(_ba103.Id)!.ProprietarioNome);
        Assert.Equal("", _m.InternosDe(_ba103.Id)!.Endereco);
    }

    [Fact]
    public async Task Entrada_invalida_nao_grava_nada()
    {
        var r = await _m.ImovelService.Salvar(new(null, Mundo.Venda("Ap")));
        Assert.Equal(TipoFalha.Validacao, r.Falha);
        Assert.Single(_m.ImoveisGravados);
        Assert.Equal(0, _m.Invalidacoes);
    }

    [Fact]
    public async Task Reordena_troca_a_capa_remove_e_apaga_so_o_arquivo_sem_uso()
    {
        var fotos = _m.FotosDe(_ba103.Id);
        var copia = Ok(await _m.ImovelService.Duplicar(_ba103.Id)); // a cópia usa os mesmos arquivos
        var nova = new FotoDesejada(null, "https://storage.teste/nova.webp", "imoveis/x/nova.webp", true);

        Ok(await _m.ImovelService.SalvarFotos(_ba103.Id, [nova, new(fotos[1].Id, fotos[1].Url, fotos[1].CaminhoNoStorage, false)]));
        var finais = _m.FotosDe(_ba103.Id);
        Assert.Equal(["https://storage.teste/nova.webp", fotos[1].Url], finais.Select(f => f.Url));
        Assert.True(finais[0].Capa);
        Assert.Empty(_m.ArquivosRemovidos); // o arquivo da foto removida ainda é usado pela cópia

        Ok(await _m.ImovelService.Excluir(copia.Id));
        Assert.Equal([fotos[0].CaminhoNoStorage!], _m.ArquivosRemovidos);
    }

    [Fact]
    public async Task Recusa_foto_de_outro_imovel()
    {
        var outro = _m.CriarImovel(Mundo.Venda("Outro"), quantasFotos: 1);
        var deOutro = _m.FotosDe(outro.Id)[0];
        var r = await _m.ImovelService.SalvarFotos(_ba103.Id, [new(deOutro.Id, deOutro.Url, deOutro.CaminhoNoStorage, true)]);
        Assert.Equal(TipoFalha.Validacao, r.Falha);
    }

    [Fact]
    public async Task Altera_status_e_destaque_e_avisa_quando_o_imovel_nao_existe()
    {
        Ok(await _m.ImovelService.AlterarStatus(_ba103.Id, StatusImovel.Vendido));
        Ok(await _m.ImovelService.AlterarDestaque(_ba103.Id, true));
        Assert.Equal(StatusImovel.Vendido, _m.Imovel(_ba103.Codigo).Status);
        Assert.True(_m.Imovel(_ba103.Codigo).Destaque);

        Assert.Equal(TipoFalha.NaoEncontrado, (await _m.ImovelService.AlterarStatus(Guid.NewGuid(), StatusImovel.Ativo)).Falha);
    }

    [Fact]
    public async Task Duplica_como_rascunho_com_as_mesmas_fotos_e_o_endereco_mas_sem_o_proprietario()
    {
        Ok(await _m.ImovelService.Salvar(new(_ba103.Id, Mundo.Venda() with { ProprietarioNome = "Maria", Endereco = "Rua A, 100" })));
        var copia = Ok(await _m.ImovelService.Duplicar(_ba103.Id));

        var imovel = _m.Imovel(copia.Codigo);
        Assert.Equal(StatusImovel.Rascunho, imovel.Status);
        Assert.Equal(2, _m.FotosDe(copia.Id).Count);
        Assert.Single(_m.FotosDe(copia.Id), f => f.Capa);
        Assert.Equal("Rua A, 100", _m.InternosDe(copia.Id)!.Endereco);
        Assert.Null(_m.InternosDe(copia.Id)!.ProprietarioNome);
    }

    [Fact]
    public async Task Exclui_o_imovel_com_as_fotos_e_os_dados_internos()
    {
        Ok(await _m.ImovelService.Salvar(new(_ba103.Id, Mundo.Venda() with { Endereco = "Rua A, 100" })));
        Ok(await _m.ImovelService.Excluir(_ba103.Id));

        Assert.Empty(_m.ImoveisGravados);
        Assert.Empty(_m.FotosGravadas);
        Assert.Null(_m.InternosDe(_ba103.Id));
        Assert.Equal(2, _m.ArquivosRemovidos.Count);
    }

    [Fact]
    public async Task Envia_a_foto_e_a_inclui_no_fim_da_lista()
    {
        var r = await _m.ImovelService.AdicionarFoto(_ba103.Id, Mundo.Jpeg());

        Assert.Equal(3, r.TotalDeFotos);
        Assert.False(_m.FotosDe(_ba103.Id)[2].Capa);
        Assert.StartsWith($"imoveis/{_ba103.Id}/", r.Imagem.CaminhoNoStorage);
        Assert.EndsWith(".webp", r.Imagem.CaminhoNoStorage);
    }

    [Fact]
    public async Task A_primeira_foto_de_um_imovel_sem_fotos_vira_capa()
    {
        var semFotos = _m.CriarImovel(Mundo.Venda("Sem fotos"));
        await _m.ImovelService.AdicionarFoto(semFotos.Id, Mundo.Jpeg());
        Assert.True(_m.FotosDe(semFotos.Id)[0].Capa);
    }

    [Fact]
    public async Task Imagens_grava_nas_pastas_previstas_e_recusa_o_resto()
    {
        Assert.StartsWith("entregas/", Ok(await _m.ImagemService.Enviar("entregas", Mundo.Jpeg())).CaminhoNoStorage);
        Assert.Equal(TipoFalha.Validacao, (await _m.ImagemService.Enviar("../segredos", Mundo.Jpeg())).Falha);
        Assert.Equal(TipoFalha.NaoEncontrado, (await _m.ImagemService.Enviar($"imoveis/{Guid.NewGuid()}", Mundo.Jpeg())).Falha);
        Assert.Equal(TipoFalha.Validacao, (await _m.ImagemService.Enviar("site", "%PDF-1.7 documento qualquer"u8.ToArray())).Falha);

        var corrompida = await _m.ImagemService.Enviar("site", Mundo.ImagemCorrompida);
        Assert.Contains("Não consegui ler essa imagem", corrompida.Mensagens[0]);
        Assert.Single(_m.ArquivosEnviados);
    }
}
