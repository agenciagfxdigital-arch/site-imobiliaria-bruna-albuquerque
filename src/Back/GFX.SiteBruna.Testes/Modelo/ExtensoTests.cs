using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Testes.Modelo;

public class ExtensoTests
{
    [Theory]
    [InlineData(349000, "trezentos e quarenta e nove mil")]
    [InlineData(420000, "quatrocentos e vinte mil")]
    [InlineData(1500, "mil e quinhentos")]
    [InlineData(1650, "mil seiscentos e cinquenta")]
    [InlineData(2300, "dois mil e trezentos")]
    [InlineData(100, "cem")]
    [InlineData(101, "cento e um")]
    [InlineData(365500, "trezentos e sessenta e cinco mil e quinhentos")]
    [InlineData(1250000, "um milhão duzentos e cinquenta mil")]
    [InlineData(2000000, "dois milhões")]
    [InlineData(1200000, "um milhão e duzentos mil")]
    [InlineData(1000500, "um milhão e quinhentos")]
    [InlineData(17, "dezessete")]
    public void Numero_por_extenso(int numero, string esperado)
    {
        Assert.Equal(esperado, Extenso.Numero(numero));
    }

    [Fact]
    public void Em_reais_com_de_nos_milhoes_redondos()
    {
        Assert.Equal("um real", Extenso.Reais(1));
        Assert.Equal("mil seiscentos e cinquenta reais", Extenso.Reais(1650));
        Assert.Equal("dois milhões de reais", Extenso.Reais(2_000_000));
    }
}
