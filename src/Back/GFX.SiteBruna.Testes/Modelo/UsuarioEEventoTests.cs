using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Interfaces;
using GFX.SiteBruna.Modelo.Regras;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class UsuarioEEventoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private const string SenhaBoa = "Videira#2026casa";

    /// <summary>Hasher de mentira: "hash:" + senha; "antigo:" simula hash com parâmetros velhos.</summary>
    private sealed class HasherDeTeste : IHasherSenha
    {
        public int Simulacoes { get; private set; }

        public string Gerar(string senha) => $"hash:{senha}";

        public VerificacaoSenha Verificar(string hash, string senha) =>
            hash == $"hash:{senha}" ? VerificacaoSenha.Confere
            : hash == $"antigo:{senha}" ? VerificacaoSenha.ConfereComHashAntigo
            : VerificacaoSenha.Falhou;

        public void SimularVerificacao(string senha) => Simulacoes++;
    }

    private readonly HasherDeTeste _hasher = new();

    [Theory]
    [InlineData("curta#1A")]
    [InlineData("semnumero#Abcdef")]
    [InlineData("SemSimbolo12345")]
    [InlineData("sem#maiuscula123")]
    public void Recusa_senha_fraca(string senha)
    {
        Assert.Throws<DomainException>(() => new Usuario("bruna@exemplo.com", "Bruna", senha, _hasher, Agora));
    }

    [Fact]
    public void Email_fica_em_minusculas_e_a_senha_so_como_hash()
    {
        var u = new Usuario(" Bruna@Exemplo.COM ", "Bruna", SenhaBoa, _hasher, Agora);
        Assert.Equal("bruna@exemplo.com", u.Email);
        Assert.Equal($"hash:{SenhaBoa}", u.HashDaSenha);
    }

    [Fact]
    public void Entrar_da_a_mesma_mensagem_para_email_inexistente_senha_errada_e_usuario_desativado()
    {
        var u = new Usuario("bruna@exemplo.com", "Bruna", SenhaBoa, _hasher, Agora);
        var semUsuario = Assert.Throws<DomainException>(() => Usuario.Entrar(null, SenhaBoa, _hasher, Agora));
        var senhaErrada = Assert.Throws<DomainException>(() => Usuario.Entrar(u, "outra", _hasher, Agora));
        u.Desativar();
        var desativado = Assert.Throws<DomainException>(() => Usuario.Entrar(u, SenhaBoa, _hasher, Agora));

        Assert.All([semUsuario, senhaErrada, desativado], e => Assert.Equal("E-mail ou senha incorretos.", e.Message));
        Assert.Equal(1, _hasher.Simulacoes); // sem usuário, gasta o tempo de uma verificação
    }

    [Fact]
    public void Entrar_registra_o_acesso_e_troca_hash_antigo()
    {
        var u = new Usuario("bruna@exemplo.com", "Bruna", SenhaBoa, _hasher, Agora);
        typeof(Usuario).GetProperty(nameof(Usuario.HashDaSenha))!.SetValue(u, $"antigo:{SenhaBoa}");

        Usuario.Entrar(u, SenhaBoa, _hasher, Agora);

        Assert.Equal(Agora, u.UltimoAcessoEm);
        Assert.Equal($"hash:{SenhaBoa}", u.HashDaSenha);
    }

    [Fact]
    public void Trocar_senha_exige_a_atual_e_uma_nova_diferente()
    {
        var u = new Usuario("bruna@exemplo.com", "Bruna", SenhaBoa, _hasher, Agora);
        Assert.Throws<DomainException>(() => u.TrocarSenha("errada", "Outra#Senha2026", _hasher));
        Assert.Throws<DomainException>(() => u.TrocarSenha(SenhaBoa, SenhaBoa, _hasher));
        u.TrocarSenha(SenhaBoa, "Outra#Senha2026", _hasher);
        Assert.Equal("hash:Outra#Senha2026", u.HashDaSenha);
    }

    // --- Eventos ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0)", Dispositivo.Mobile)]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-X200)", Dispositivo.Tablet)]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) Mobile", Dispositivo.Mobile)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64)", Dispositivo.Desktop)]
    public void Dispositivo_pelo_user_agent(string userAgent, Dispositivo esperado)
    {
        Assert.Equal(esperado, Navegador.DispositivoDe(userAgent));
    }

    [Theory]
    [InlineData("WhatsApp/2.23.20.0")]
    [InlineData("facebookexternalhit/1.1")]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1)")]
    public void Robos_e_previas_de_link_nao_contam(string userAgent)
    {
        Assert.True(Navegador.EhRobo(userAgent));
    }

    [Fact]
    public void Evento_exige_visitante_valido_e_imovel_na_visualizacao()
    {
        var ok = new Evento(new DadosDoEvento { Tipo = TipoEvento.WhatsappClick, VisitanteId = "abc12345-xyz" }, Dispositivo.Mobile, Agora);
        Assert.Equal(TipoEvento.WhatsappClick, ok.Tipo);

        Assert.Throws<DomainException>(() =>
            new Evento(new DadosDoEvento { Tipo = TipoEvento.PageView, VisitanteId = "curto" }, Dispositivo.Mobile, Agora));
        Assert.Throws<DomainException>(() =>
            new Evento(new DadosDoEvento { Tipo = TipoEvento.PropertyView, VisitanteId = "abc12345-xyz" }, Dispositivo.Mobile, Agora));
    }

    [Fact]
    public void Evento_corta_textos_longos_em_vez_de_recusar()
    {
        var e = new Evento(new DadosDoEvento
        {
            Tipo = TipoEvento.PageView, VisitanteId = "abc12345-xyz", Pagina = "/" + new string('a', 600), UtmSource = "  ",
        }, Dispositivo.Desktop, Agora);
        Assert.Equal(500, e.Pagina!.Length);
        Assert.Null(e.UtmSource);
    }
}
