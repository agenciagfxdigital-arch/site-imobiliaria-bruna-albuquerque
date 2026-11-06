using GFX.SiteBruna.Aplicacao.Authorization;
using GFX.SiteBruna.Aplicacao.DTOs;

namespace GFX.SiteBruna.Testes.Aplicacao;

public class OperadorServiceTests
{
    private readonly Mundo _m = new();

    [Fact]
    public async Task Cadastra_com_o_telefone_normalizado()
    {
        var salvo = await _m.OperadorService.Salvar(new("(49) 99999-1234", "  Bruna ", true));

        Assert.True(salvo.Sucesso, string.Join(" ", salvo.Mensagens));
        Assert.Equal("5549999991234", salvo.Dados!.Telefone);
        Assert.Equal("Bruna", salvo.Dados.Nome);
        Assert.Contains((await _m.OperadorService.Listar()).Dados!, o => o.Telefone == "5549999991234");
    }

    [Fact]
    public async Task Mesmo_telefone_atualiza_em_vez_de_duplicar_e_pode_desativar()
    {
        await _m.OperadorService.Salvar(new("5549999991234", "Bruna", true));

        var salvo = await _m.OperadorService.Salvar(new("49 99999-1234", "Bruna Albuquerque", false));

        Assert.False(salvo.Dados!.Ativo);
        var operador = Assert.Single((await _m.OperadorService.Listar()).Dados!, o => o.Telefone == "5549999991234");
        Assert.Equal("Bruna Albuquerque", operador.Nome);
        Assert.False(operador.Ativo);
    }

    [Fact]
    public async Task Telefone_invalido_e_erro_de_validacao_e_nao_derruba_a_sessao()
    {
        var salvo = await _m.OperadorService.Salvar(new("123", "Bruna", true));

        Assert.False(salvo.Sucesso);
        Assert.NotEqual(TipoFalha.NaoAutorizado, salvo.Falha);
        Assert.Contains("DDD", salvo.Mensagens[0]);
    }
}
