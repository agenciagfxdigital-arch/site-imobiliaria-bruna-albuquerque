using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GFX.SiteBruna.Aplicacao.DTOs.Usuarios;
using GFX.SiteBruna.Aplicacao.Services;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GFX.SiteBruna.Testes.Integracao;

/// <summary>HTTP de ponta a ponta: login, proteção das rotas do painel, site público e webhook do WhatsApp.</summary>
public class ApiTests(ApiDeTeste api) : IClassFixture<ApiDeTeste>
{
    private const string Senha = "Videira#2026casa";

    private async Task<string> Entrar()
    {
        var email = $"bruna.{Guid.NewGuid():N}@exemplo.com";
        await using (var escopo = api.Servicos.CreateAsyncScope())
            Assert.True((await escopo.ServiceProvider.GetRequiredService<UsuarioService>().Criar(new(email, "Bruna", Senha))).Sucesso);

        var resposta = await api.Cliente.PostAsJsonAsync("/api/Auth/entrar", new EntrarRequest(email, Senha));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("dados").GetProperty("token").GetString()!;
    }

    private static HttpRequestMessage Com(HttpMethod metodo, string url, string? token = null)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        if (token is not null) requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return requisicao;
    }

    [Theory]
    [InlineData("/api/Imovel")]
    [InlineData("/api/Usuario")]
    [InlineData("/api/Auth/eu")]
    public async Task Rotas_do_painel_exigem_login(string rota)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente.GetAsync(rota)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente.SendAsync(Com(HttpMethod.Get, rota, "token-falso"))).StatusCode);
    }

    [Fact]
    public async Task Login_errado_responde_401_com_mensagem_em_portugues()
    {
        var resposta = await api.Cliente.PostAsJsonAsync("/api/Auth/entrar", new EntrarRequest("ninguem@exemplo.com", Senha));
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Contains("E-mail ou senha incorretos.", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Com_token_o_painel_abre_e_usuario_desativado_perde_o_acesso_na_hora()
    {
        var token = await Entrar();
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente.SendAsync(Com(HttpMethod.Get, "/api/Imovel", token))).StatusCode);

        var eu = await api.Cliente.SendAsync(Com(HttpMethod.Get, "/api/Auth/eu", token));
        var id = JsonDocument.Parse(await eu.Content.ReadAsStringAsync()).RootElement.GetProperty("dados").GetProperty("id").GetGuid();
        await using (var escopo = api.Servicos.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Usuario.SingleAsync(u => u.Id == id)).Desativar();
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente.SendAsync(Com(HttpMethod.Get, "/api/Imovel", token))).StatusCode);
    }

    [Fact]
    public async Task Salva_imovel_pelo_painel_e_erro_de_negocio_vira_400_em_portugues()
    {
        var token = await Entrar();
        var invalido = Com(HttpMethod.Post, "/api/Imovel", token);
        invalido.Content = JsonContent.Create(new { dados = new { finalidade = "Venda", tipo = "Casa", titulo = "Ap", status = "Rascunho" } });
        var erro = await api.Cliente.SendAsync(invalido);
        Assert.Equal(HttpStatusCode.BadRequest, erro.StatusCode);
        Assert.Contains("Escreva um título.", await erro.Content.ReadAsStringAsync());

        var valido = Com(HttpMethod.Post, "/api/Imovel", token);
        valido.Content = JsonContent.Create(new
        {
            dados = new { finalidade = "Venda", tipo = "Casa", titulo = "Casa com quintal", preco = 420000, status = "Rascunho", endereco = "Rua A, 100" },
        });
        var salvo = await api.Cliente.SendAsync(valido);
        Assert.Equal(HttpStatusCode.OK, salvo.StatusCode);
        var id = JsonDocument.Parse(await salvo.Content.ReadAsStringAsync()).RootElement.GetProperty("dados").GetProperty("id").GetGuid();

        var detalhe = await (await api.Cliente.SendAsync(Com(HttpMethod.Get, $"/api/Imovel/{id}", token))).Content.ReadAsStringAsync();
        Assert.Contains("Rua A, 100", detalhe); // o painel vê o endereço exato
    }

    [Fact]
    public async Task Site_publico_abre_sem_login_e_eventos_respondem_204()
    {
        var configuracoes = await api.Cliente.GetAsync("/api/Site/configuracoes");
        Assert.Equal(HttpStatusCode.OK, configuracoes.StatusCode);
        Assert.Contains("CRECI/SC 60723", await configuracoes.Content.ReadAsStringAsync());
        Assert.Contains("Seu próximo imóvel começa com uma boa escolha.", await api.Cliente.GetStringAsync("/api/Site/paginas"));

        var evento = await api.Cliente.PostAsJsonAsync("/api/Site/eventos", new { tipo = "WhatsappClick", visitanteId = "visitante-12345" });
        Assert.Equal(HttpStatusCode.NoContent, evento.StatusCode);
    }

    [Fact]
    public async Task Imoveis_do_site_abrem_sem_login_com_filtros_da_query_e_rascunho_responde_404()
    {
        var token = await Entrar();
        var novo = Com(HttpMethod.Post, "/api/Imovel", token);
        novo.Content = JsonContent.Create(new
        {
            dados = new { finalidade = "Aluguel", tipo = "Casa", titulo = "Casa com pátio para alugar", aluguel = 1800, status = "Rascunho", endereco = "Rua B, 200" },
        });
        var salvo = JsonDocument.Parse(await (await api.Cliente.SendAsync(novo)).Content.ReadAsStringAsync()).RootElement.GetProperty("dados");
        var (id, codigo) = (salvo.GetProperty("id").GetGuid(), salvo.GetProperty("codigo").GetString()!);

        Assert.Equal(HttpStatusCode.NotFound, (await api.Cliente.GetAsync($"/api/Site/imovel/{codigo}")).StatusCode);
        var previa = await api.Cliente.SendAsync(Com(HttpMethod.Get, $"/api/Imovel/{id}/previa", token));
        Assert.Equal(HttpStatusCode.OK, previa.StatusCode);

        var publicar = Com(HttpMethod.Put, $"/api/Imovel/{id}/status", token);
        publicar.Content = JsonContent.Create("Ativo");
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente.SendAsync(publicar)).StatusCode);

        var lista = await api.Cliente.GetStringAsync("/api/Site/imoveis?finalidade=Aluguel&tipo=Casa&precoMaximo=2000&ordem=MenorPreco&pagina=1");
        Assert.Contains(codigo, lista);
        Assert.DoesNotContain(codigo, await api.Cliente.GetStringAsync("/api/Site/imoveis?finalidade=Aluguel&precoMaximo=1000"));

        var pagina = await api.Cliente.GetStringAsync($"/api/Site/imovel/{codigo.ToLowerInvariant()}-casa-com-patio");
        Assert.Contains("Casa com pátio para alugar", pagina);
        Assert.DoesNotContain("Rua B", pagina); // o site nunca recebe o endereço exato
        Assert.Contains(codigo.ToLowerInvariant(), await api.Cliente.GetStringAsync("/api/Site/sitemap"));
    }

    [Fact]
    public async Task Comando_cria_o_primeiro_usuario_e_recusa_senhas_diferentes()
    {
        var email = $"primeiro.{Guid.NewGuid():N}@exemplo.com";
        var entradaOriginal = Console.In;
        try
        {
            Console.SetIn(new StringReader($"{Senha}\nOutra#Senha2026\n"));
            Assert.Equal(1, await GFX.SiteBruna.Api.Comandos.CriarUsuario.Executar(api.Servicos, ["criar-usuario", email, "Bruna"]));

            Console.SetIn(new StringReader($"{Senha}\n{Senha}\n"));
            Assert.Equal(0, await GFX.SiteBruna.Api.Comandos.CriarUsuario.Executar(api.Servicos, ["criar-usuario", email, "Bruna"]));
        }
        finally
        {
            Console.SetIn(entradaOriginal);
        }
        var login = await api.Cliente.PostAsJsonAsync("/api/Auth/entrar", new EntrarRequest(email, Senha));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Webhook_verifica_o_token_da_Meta()
    {
        var certo = await api.Cliente.GetAsync($"/api/whatsapp/webhook?hub.mode=subscribe&hub.verify_token={ApiDeTeste.VerifyToken}&hub.challenge=123");
        Assert.Equal("123", await certo.Content.ReadAsStringAsync());
        var errado = await api.Cliente.GetAsync("/api/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=outro&hub.challenge=123");
        Assert.Equal(HttpStatusCode.Forbidden, errado.StatusCode);
    }

    [Fact]
    public async Task Webhook_recusa_assinatura_errada_e_processa_a_mensagem_assinada()
    {
        const string telefone = "5549970000077";
        await using (var escopo = api.Servicos.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Add(new Operador(telefone, "Bruna", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        var corpo = Encoding.UTF8.GetBytes($$"""
            { "entry": [{ "changes": [{ "value": { "metadata": { "phone_number_id": "111" }, "messages": [
              { "from": "{{telefone}}", "id": "wamid.api.1", "timestamp": "1792500000", "type": "text", "text": { "body": "ajuda" } }
            ] } }] }] }
            """);

        var semAssinatura = new HttpRequestMessage(HttpMethod.Post, "/api/whatsapp/webhook") { Content = new ByteArrayContent(corpo) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Cliente.SendAsync(semAssinatura)).StatusCode);

        var assinada = new HttpRequestMessage(HttpMethod.Post, "/api/whatsapp/webhook") { Content = new ByteArrayContent(corpo) };
        assinada.Headers.Add("X-Hub-Signature-256",
            $"sha256={Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(ApiDeTeste.AppSecret), corpo))}");
        Assert.Equal(HttpStatusCode.OK, (await api.Cliente.SendAsync(assinada)).StatusCode);

        // Processamento em segundo plano: a mensagem sai de "pendente"
        StatusMensagem? status = null;
        for (var tentativa = 0; tentativa < 50 && status != StatusMensagem.Processada; tentativa++)
        {
            await Task.Delay(100);
            await using var escopo = api.Servicos.CreateAsyncScope();
            status = (await escopo.ServiceProvider.GetRequiredService<AppDbContext>().MensagemDoWhatsapp.AsNoTracking()
                .SingleAsync(m => m.Wamid == "wamid.api.1")).Status;
        }
        Assert.Equal(StatusMensagem.Processada, status);
    }
}
