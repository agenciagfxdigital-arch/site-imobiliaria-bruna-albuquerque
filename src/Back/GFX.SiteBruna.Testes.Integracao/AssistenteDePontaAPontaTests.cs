using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>O assistente com o banco de verdade: ficha → rascunho → fotos (SkiaSharp de verdade) → publicar → desfazer.</summary>
public class AssistenteDePontaAPontaTests(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    private const string Telefone = "5549970000099";

    [Fact]
    public async Task Ficha_fotos_publicacao_e_desfazer()
    {
        await Preparar();

        await Enviar(new ConteudoMensagem.Texto("""
            FICHA DO IMÓVEL
            Finalidade: venda
            Tipo: casa
            Título: Casa 3 quartos com quintal
            Bairro: centro
            Preço: 420 mil
            Endereço (não aparece no site): Rua A, 100
            """));
        var rascunho = await Ler(db => db.Imovel.AsNoTracking().SingleAsync(i => i.Titulo == "Casa 3 quartos com quintal"));
        Assert.Equal(StatusImovel.Rascunho, rascunho.Status);
        Assert.Equal("casa-3-quartos-com-quintal-centro", rascunho.Slug);
        Assert.Equal("Rua A, 100", (await Ler(db => db.DadosInternosImovel.AsNoTracking().SingleAsync(d => d.ImovelId == rascunho.Id))).Endereco);

        await Enviar(new ConteudoMensagem.Imagem("m1", "image/jpeg", null), new ConteudoMensagem.Imagem("m2", "image/jpeg", null));
        var fotos = await Ler(db => db.FotoImovel.AsNoTracking().Where(f => f.ImovelId == rascunho.Id).OrderBy(f => f.Ordem).ToListAsync());
        Assert.Equal(2, fotos.Count);
        Assert.True(fotos[0].Capa);
        Assert.All(fotos, f => Assert.EndsWith(".webp", f.CaminhoNoStorage));
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(banco.Storage.Arquivos[fotos[0].CaminhoNoStorage!][..4]));

        var publicar = banco.Canal.Para(Telefone).Last();
        Assert.Equal(["Publicar", "Agora não"], publicar.Botoes!.Select(b => b.Titulo));
        await Enviar(new ConteudoMensagem.Botao(publicar.Botoes![0].Id, "Publicar"));
        Assert.Equal(StatusImovel.Ativo, (await Ler(db => db.Imovel.AsNoTracking().SingleAsync(i => i.Id == rascunho.Id))).Status);
        Assert.Contains($"https://site.teste/imovel/{rascunho.Codigo.ToLowerInvariant()}-", banco.Canal.Para(Telefone).Last().Texto);

        // Desfazer lê o estado "antes" gravado em jsonb na auditoria
        await using (var escopo = banco.Escopo())
        {
            var integracao = escopo.ServiceProvider.GetRequiredService<IntegracaoService>();
            var op = (await integracao.OperadorAutorizado(Telefone))!;
            var desfazer = await integracao.ProporDesfazer(op);
            await integracao.Confirmar(op, desfazer.AcaoId);
        }
        Assert.Equal(StatusImovel.Rascunho, (await Ler(db => db.Imovel.AsNoTracking().SingleAsync(i => i.Id == rascunho.Id))).Status);
        Assert.All(await Ler(db => db.MensagemDoWhatsapp.AsNoTracking().ToListAsync()),
            m => Assert.Equal(StatusMensagem.Processada, m.Status));
    }

    private async Task Preparar()
    {
        await using var escopo = banco.Escopo();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(new Bairro("Centro"), new Operador(Telefone, "Bruna", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }

    /// <summary>Como o webhook: grava num escopo e processa em outro.</summary>
    private async Task Enviar(params ConteudoMensagem[] conteudos)
    {
        IReadOnlyList<string> telefones;
        await using (var escopo = banco.Escopo())
        {
            var mensagens = conteudos.Select(c => new MensagemRecebida($"wamid.{Guid.NewGuid():N}", Telefone, DateTimeOffset.UtcNow, c)).ToList();
            telefones = await escopo.ServiceProvider.GetRequiredService<AssistenteService>().Receber(mensagens);
        }
        foreach (var telefone in telefones)
        {
            await using var escopo = banco.Escopo();
            await escopo.ServiceProvider.GetRequiredService<AssistenteService>().ProcessarFila(telefone);
        }
    }

    private async Task<T> Ler<T>(Func<AppDbContext, Task<T>> consulta)
    {
        await using var escopo = banco.Escopo();
        return await consulta(escopo.ServiceProvider.GetRequiredService<AppDbContext>());
    }
}
