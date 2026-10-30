using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Testes.Modelo;

public class FichaTests
{
    private const string FichaVenda = """
        FICHA DO IMÓVEL
        Finalidade: venda
        Tipo: apto
        Título: Apartamento 2 quartos com sacada no Centro
        Bairro: centro
        Preço: 365 mil
        Quartos: 2
        Suítes:
        Banheiros: 1
        Vagas: 1
        Área: 74,5 m²
        Situação: na planta
        Previsão de entrega: dezembro de 2027
        Minha Casa Minha Vida: sim
        Aceita FGTS: Sim
        Entrada parcelada: não
        Diferenciais: sacada com churrasqueira, Elevador, Vista para o parque
        Proprietário (não aparece no site): Maria da Silva
        Endereço (não aparece no site): Rua A, 100
        Descrição:
        Apartamento novo, perto de tudo.

        Sol da manhã nos quartos.
        """;

    [Theory]
    [InlineData("365 mil", 365000)]
    [InlineData("1,6 mil", 1600)]
    [InlineData("R$ 365.000,00", 365000)]
    [InlineData("1.650", 1650)]
    [InlineData("1.650,50", 1650.5)]
    [InlineData("1,2 milhão", 1200000)]
    [InlineData("365000", 365000)]
    public void Entende_os_jeitos_comuns_de_escrever_valores(string texto, double esperado)
    {
        Assert.Equal((decimal)esperado, Ficha.LerDinheiro(texto));
    }

    [Fact]
    public void Nao_entende_valor_sem_numero()
    {
        Assert.Null(Ficha.LerDinheiro("a combinar"));
    }

    [Fact]
    public void Converte_a_ficha_de_venda_nos_campos_do_imovel()
    {
        var f = Ficha.Ler(FichaVenda);

        Assert.Empty(f.Problemas);
        Assert.Equal("centro", f.Bairro);
        Assert.Null(f.Codigo);
        var c = f.Campos;
        Assert.Equal(Finalidade.Venda, c.Finalidade);
        Assert.Equal(TipoImovel.Apartamento, c.Tipo);
        Assert.Equal("Apartamento 2 quartos com sacada no Centro", c.Titulo);
        Assert.Equal(365000m, c.Preco);
        Assert.Equal(2, c.Quartos);
        Assert.Null(c.Suites);
        Assert.Equal(1, c.Banheiros);
        Assert.Equal(1, c.Vagas);
        Assert.Equal(74.5m, c.Area);
        Assert.Equal(Situacao.NaPlanta, c.Situacao);
        Assert.Equal("dezembro de 2027", c.PrevisaoEntrega);
        Assert.True(c.Mcmv);
        Assert.True(c.AceitaFgts);
        Assert.False(c.EntradaParcelada);
        Assert.Equal(["Sacada com churrasqueira", "Elevador", "Vista para o parque"], c.Diferenciais);
        Assert.Equal("Maria da Silva", c.ProprietarioNome);
        Assert.Equal("Rua A, 100", c.Endereco);
        Assert.Equal("Apartamento novo, perto de tudo.\n\nSol da manhã nos quartos.", c.Descricao);
        Assert.Null(c.Aluguel);
    }

    [Fact]
    public void Aceita_rotulos_sem_acento_e_garantias_com_o_nome_oficial()
    {
        var f = Ficha.Ler("Finalidade: alugar\nTipo: casa\nTitulo: Casa com quintal\nAluguel: 1.650\nGarantias: caucao, seguro-fianca");

        Assert.Empty(f.Problemas);
        Assert.Equal(Finalidade.Aluguel, f.Campos.Finalidade);
        Assert.Equal(1650m, f.Campos.Aluguel);
        Assert.Equal(["Caução", "Seguro-fiança"], f.Campos.Garantias);
    }

    [Fact]
    public void Lista_todos_os_problemas_de_uma_vez()
    {
        var f = Ficha.Ler("FICHA DO IMÓVEL\nFinalidade: permuta\nPreço: a combinar\nQuartos: dois\nPiscina: sim\nGarantias: cheque");

        Assert.Equal(
            [
                "Finalidade: use venda ou aluguel.",
                "Preço: não entendi \"a combinar\". Use, por exemplo, 365 mil ou 1.650,00.",
                "Quartos: use só o número (ex.: 2).",
                "Não reconheci o campo \"Piscina\".",
                "Garantias: use Caução, Fiador, Seguro-fiança, Título de capitalização.",
                "Faltou preencher: Finalidade, Tipo, Título.",
            ],
            f.Problemas);
    }

    [Fact]
    public void Com_codigo_e_atualizacao_e_nao_exige_finalidade_tipo_nem_titulo()
    {
        var f = Ficha.Ler("FICHA DO IMÓVEL\nCódigo: ba107\nPreço: 349 mil");

        Assert.Equal("BA107", f.Codigo);
        Assert.Equal(349000m, f.Campos.Preco);
        Assert.Empty(f.Problemas);
        Assert.Null(f.Bairro);
    }

    [Theory]
    [InlineData("*Ficha do imóvel*\nTítulo: x", true)]
    [InlineData("Tipo: casa\nTítulo: Casa\nPreço: 300 mil", true)]
    [InlineData("Oi, tudo bem? Preço: 300 mil", false)]
    public void Reconhece_ficha_pelo_cabecalho_ou_por_varios_rotulos(string texto, bool esperado)
    {
        Assert.Equal(esperado, Ficha.EhFicha(texto));
    }

    [Theory]
    [InlineData(Finalidade.Venda)]
    [InlineData(Finalidade.Aluguel)]
    public void Os_modelos_enviados_sao_lidos_sem_problemas_alem_do_titulo(Finalidade finalidade)
    {
        var f = Ficha.Ler(Ficha.Modelo(finalidade));

        Assert.Equal(["Faltou preencher: Título."], f.Problemas);
        Assert.Equal(finalidade, f.Campos.Finalidade);
    }
}
