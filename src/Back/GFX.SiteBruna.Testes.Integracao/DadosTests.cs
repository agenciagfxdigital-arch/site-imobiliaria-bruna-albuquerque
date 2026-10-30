using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GFX.SiteBruna.Testes.Integracao;

public class DadosTests(BancoDeTeste banco) : IClassFixture<BancoDeTeste>
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.UtcNow;

    private static DadosDoImovel Dados(string titulo = "Sala comercial no Centro") => new()
    {
        Finalidade = Finalidade.Aluguel,
        Tipo = TipoImovel.SalaComercial,
        Titulo = titulo,
        Aluguel = 1_650.50m,
        Situacao = Situacao.NaPlanta,
        Diferenciais = ["Elevador", "Portaria"],
        Garantias = ["Caução"],
        Status = StatusImovel.Ativo,
    };

    [Fact]
    public async Task Tabelas_ficam_no_schema_site_e_nada_no_public()
    {
        await using var escopo = banco.Escopo();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        var noPublic = await db.Database.SqlQueryRaw<string>(
            "select table_name as \"Value\" from information_schema.tables where table_schema = 'public'").ToListAsync();
        var noSite = await db.Database.SqlQueryRaw<string>(
            "select table_name as \"Value\" from information_schema.tables where table_schema = 'site'").ToListAsync();

        Assert.Empty(noPublic);
        Assert.Contains("properties", noSite);
        Assert.Contains("__EFMigrationsHistory", noSite);
    }

    [Fact]
    public async Task Grava_e_le_o_imovel_com_enums_em_texto_listas_e_valores()
    {
        Guid id;
        await using (var escopo = banco.Escopo())
        {
            var imoveis = escopo.ServiceProvider.GetRequiredService<IImovelRepository>();
            var imovel = new Imovel(Dados(), await imoveis.ProximoCodigo(), null, OrigemImovel.Painel, Agora);
            imoveis.Add(imovel);
            await escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().Commit();
            id = imovel.Id;
        }

        await using var leitura = banco.Escopo();
        var db = leitura.ServiceProvider.GetRequiredService<AppDbContext>();
        var lido = await db.Imovel.AsNoTracking().SingleAsync(i => i.Id == id);
        Assert.Equal(TipoImovel.SalaComercial, lido.Tipo);
        Assert.Equal(Situacao.NaPlanta, lido.Situacao);
        Assert.Equal(1_650.50m, lido.Aluguel);
        Assert.Equal(["Elevador", "Portaria"], lido.Diferenciais);
        Assert.StartsWith("BA", lido.Codigo);

        var tipoNoBanco = await db.Database.SqlQueryRaw<string>(
            "select tipo as \"Value\" from site.properties where id = {0}", id).SingleAsync();
        Assert.Equal("sala_comercial", tipoNoBanco);
    }

    [Fact]
    public async Task Codigo_repetido_vira_erro_de_negocio()
    {
        await using var escopo = banco.Escopo();
        var imoveis = escopo.ServiceProvider.GetRequiredService<IImovelRepository>();
        imoveis.Add(new Imovel(Dados() with { Codigo = "DUP1" }, null, null, OrigemImovel.Painel, Agora));
        imoveis.Add(new Imovel(Dados() with { Codigo = "DUP1" }, null, null, OrigemImovel.Painel, Agora));

        var erro = await Assert.ThrowsAsync<DomainException>(() => escopo.ServiceProvider.GetRequiredService<IUnitOfWork>().Commit());
        Assert.Equal(CodigoErro.CodigoDuplicado, erro.Codigo);
    }

    [Fact]
    public async Task Excluir_imovel_leva_fotos_e_dados_internos_e_preserva_a_auditoria()
    {
        await using var escopo = banco.Escopo();
        var sp = escopo.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var imovel = new Imovel(Dados(), await sp.GetRequiredService<IImovelRepository>().ProximoCodigo(), null, OrigemImovel.Painel, Agora);
        var operador = new Operador("5549970000001", "Teste", Agora);
        db.AddRange(imovel, operador, new FotoImovel(imovel.Id, "u", "imoveis/x/a.webp", 0, true),
            new DadosInternosImovel(imovel.Id, Agora),
            RegistroDeAuditoria.Sucedido(operador.Id, RegistroDeAuditoria.CriarRascunho, imovel.Id, Agora));
        await db.SaveChangesAsync();

        db.Remove(imovel);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.False(await db.FotoImovel.AnyAsync(f => f.ImovelId == imovel.Id));
        Assert.False(await db.DadosInternosImovel.AnyAsync(d => d.ImovelId == imovel.Id));
        var auditoria = await db.RegistroDeAuditoria.SingleAsync(r => r.OperadorId == operador.Id);
        Assert.Null(auditoria.ImovelId);
    }

    [Fact]
    public async Task Reserva_do_operador_e_atomica_e_renovavel_pelo_mesmo_token()
    {
        await using var escopo = banco.Escopo();
        var sp = escopo.ServiceProvider;
        var operador = new Operador("5549970000002", "Teste", Agora);
        var repo = sp.GetRequiredService<IOperadorRepository>();
        repo.Add(operador);
        await sp.GetRequiredService<IUnitOfWork>().Commit();

        Assert.True(await repo.Reservar(operador.Id, "a", Agora.AddMinutes(2), Agora));
        Assert.False(await repo.Reservar(operador.Id, "b", Agora.AddMinutes(2), Agora));
        Assert.True(await repo.Reservar(operador.Id, "a", Agora.AddMinutes(4), Agora));
        await repo.Liberar(operador.Id, "b"); // token errado não libera
        Assert.False(await repo.Reservar(operador.Id, "b", Agora.AddMinutes(2), Agora));
        await repo.Liberar(operador.Id, "a");
        Assert.True(await repo.Reservar(operador.Id, "b", Agora.AddMinutes(2), Agora));
    }

    [Fact]
    public async Task Fila_recusa_wamid_repetido_e_guarda_o_tipo_do_conteudo()
    {
        await using var escopo = banco.Escopo();
        var sp = escopo.ServiceProvider;
        var operador = new Operador("5549970000003", "Teste", Agora);
        sp.GetRequiredService<IOperadorRepository>().Add(operador);
        await sp.GetRequiredService<IUnitOfWork>().Commit();
        var fila = sp.GetRequiredService<IFilaDeMensagens>();
        var recebida = new MensagemRecebida("wamid.repetido", operador.Telefone, Agora,
            new ConteudoMensagem.Imagem("midia-1", "image/jpeg", "BA101"));

        Assert.True(await fila.Inserir(MensagemDoWhatsapp.ParaProcessar(recebida, operador.Id)));
        Assert.False(await fila.Inserir(MensagemDoWhatsapp.ParaProcessar(recebida, operador.Id)));

        var proxima = await fila.ProximaPendente(operador.Id);
        Assert.Equal(new ConteudoMensagem.Imagem("midia-1", "image/jpeg", "BA101"), proxima!.Conteudo);
        proxima.MarcarProcessada();
        await fila.Gravar(proxima);
        Assert.Null(await fila.ProximaPendente(operador.Id));
    }
}
