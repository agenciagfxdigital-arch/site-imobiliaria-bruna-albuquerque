using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Testes.Modelo;

public class ImovelTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static DadosDoImovel Dados() => new()
    {
        Codigo = "",
        Finalidade = Finalidade.Venda,
        Tipo = TipoImovel.Apartamento,
        Titulo = "Apartamento 2 quartos",
        Preco = 250_000,
        Aluguel = 1_500,
        EntradaAPartir = 15_000,
        Quartos = 2,
        Banheiros = 1,
        Vagas = 1,
        Area = 55,
        Situacao = Situacao.Pronto,
        Status = StatusImovel.Rascunho,
    };

    private static Imovel Novo(DadosDoImovel dados, string? bairro = null) =>
        new(dados, "BA101", bairro, OrigemImovel.Painel, Agora);

    // --- Validação -------------------------------------------------------------------------

    [Fact]
    public void Aceita_uma_entrada_completa()
    {
        Assert.Equal("Apartamento 2 quartos", Novo(Dados()).Titulo);
    }

    [Fact]
    public void Recusa_titulo_curto_com_mensagem_para_o_usuario()
    {
        var erro = Assert.Throws<DomainException>(() => Novo(Dados() with { Titulo = "Ap" }));
        Assert.Equal(CodigoErro.Validacao, erro.Codigo);
        Assert.Equal("Escreva um título.", erro.Message);
    }

    [Theory]
    [InlineData("BA 101")]
    [InlineData("BA-101")]
    [InlineData("B")]
    public void Recusa_codigo_com_espaco_simbolo_ou_curto(string codigo)
    {
        Assert.Throws<DomainException>(() => Novo(Dados() with { Codigo = codigo }));
    }

    [Fact]
    public void Recusa_preco_negativo()
    {
        var erro = Assert.Throws<DomainException>(() => Novo(Dados() with { Preco = -1 }));
        Assert.Equal("O preço não pode ser negativo.", erro.Message);
    }

    [Fact]
    public void Recusa_nome_do_proprietario_com_mais_de_120_letras()
    {
        Assert.Throws<DomainException>(() => Novo(Dados() with { ProprietarioNome = new string('a', 121) }));
    }

    // --- Campos por finalidade e código ------------------------------------------------------

    [Fact]
    public void Em_venda_limpa_o_aluguel_e_mantem_preco_e_entrada()
    {
        var imovel = Novo(Dados() with { Finalidade = Finalidade.Venda });
        Assert.Equal(250_000, imovel.Preco);
        Assert.Equal(15_000, imovel.EntradaAPartir);
        Assert.Null(imovel.Aluguel);
    }

    [Fact]
    public void Em_aluguel_limpa_preco_de_venda_e_entrada()
    {
        var imovel = Novo(Dados() with { Finalidade = Finalidade.Aluguel });
        Assert.Equal(1_500, imovel.Aluguel);
        Assert.Null(imovel.Preco);
        Assert.Null(imovel.EntradaAPartir);
    }

    [Fact]
    public void Codigo_informado_fica_em_maiusculas_e_vazio_usa_o_gerado()
    {
        Assert.Equal("BA150", Novo(Dados() with { Codigo = "ba150" }).Codigo);
        Assert.Equal("BA101", Novo(Dados() with { Codigo = "" }).Codigo);
    }

    [Fact]
    public void Atualizar_sem_codigo_mantem_o_codigo_atual()
    {
        var imovel = Novo(Dados());
        imovel.Atualizar(Dados() with { Titulo = "Apartamento reformado" }, null, Agora);
        Assert.Equal("BA101", imovel.Codigo);
        Assert.Equal("Apartamento reformado", imovel.Titulo);
    }

    // --- Slug ----------------------------------------------------------------------------------

    [Theory]
    [InlineData("Casa 2 quartos com quintal", "Santa Gema", "casa-2-quartos-com-quintal-santa-gema")]
    [InlineData("Apartamento no Centro", "Centro", "apartamento-no-centro")]
    [InlineData("Apê no Universitário", null, "ape-no-universitario")]
    public void Slug_acrescenta_o_bairro_so_quando_o_titulo_nao_cita(string titulo, string? bairro, string esperado)
    {
        Assert.Equal(esperado, Imovel.GerarSlug(titulo, bairro));
    }

    // --- Status e selo de vendido (antes na trigger do banco) ---------------------------------

    [Fact]
    public void Ao_vender_marca_a_data_e_ao_reativar_apaga()
    {
        var imovel = Novo(Dados() with { Status = StatusImovel.Ativo });
        imovel.AlterarStatus(StatusImovel.Vendido, Agora);
        Assert.Equal(Agora, imovel.VendidoEm);

        imovel.AlterarStatus(StatusImovel.Ativo, Agora.AddDays(1));
        Assert.Null(imovel.VendidoEm);
    }

    [Fact]
    public void Salvar_de_novo_como_vendido_nao_muda_a_data_da_venda()
    {
        var imovel = Novo(Dados() with { Status = StatusImovel.Vendido });
        imovel.Atualizar(Dados() with { Status = StatusImovel.Vendido, Titulo = "Outro título" }, null, Agora.AddDays(3));
        Assert.Equal(Agora, imovel.VendidoEm);
    }

    [Fact]
    public void Visivel_no_site_quando_ativo_ou_vendido_dentro_do_prazo_do_selo()
    {
        var ativo = Novo(Dados() with { Status = StatusImovel.Ativo });
        var vendido = Novo(Dados() with { Status = StatusImovel.Vendido });
        var rascunho = Novo(Dados());

        Assert.True(ativo.EstaVisivelNoSite(Agora, diasSeloVendido: 30));
        Assert.True(vendido.EstaVisivelNoSite(Agora.AddDays(29), diasSeloVendido: 30));
        Assert.False(vendido.EstaVisivelNoSite(Agora.AddDays(31), diasSeloVendido: 30));
        Assert.False(vendido.EstaVisivelNoSite(Agora, diasSeloVendido: 0));
        Assert.False(rascunho.EstaVisivelNoSite(Agora, diasSeloVendido: 30));
    }

    // --- Cópia ---------------------------------------------------------------------------------

    [Fact]
    public void Copia_vira_rascunho_sai_dos_destaques_e_marca_o_titulo()
    {
        var original = Novo(Dados() with { Status = StatusImovel.Ativo, Destaque = true });
        var copia = original.Copiar("BA102", Agora);

        Assert.Equal(StatusImovel.Rascunho, copia.Status);
        Assert.False(copia.Destaque);
        Assert.Equal("Apartamento 2 quartos (cópia)", copia.Titulo);
        Assert.Equal("BA102", copia.Codigo);
        Assert.NotEqual(original.Id, copia.Id);
    }

    [Fact]
    public void Copia_limita_o_titulo_a_140_caracteres_e_nao_divide_as_listas()
    {
        var original = Novo(Dados() with { Titulo = new string('x', 140), Diferenciais = ["Sacada"] });
        var copia = original.Copiar("BA102", Agora);

        Assert.Equal(140, copia.Titulo.Length);
        Assert.NotSame(original.Diferenciais, copia.Diferenciais);
    }
}
