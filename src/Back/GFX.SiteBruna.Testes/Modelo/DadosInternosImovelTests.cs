using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Testes.Modelo;

public class DadosInternosImovelTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static DadosInternosImovel Com(string proprietario, string endereco)
    {
        var internos = new DadosInternosImovel(Guid.CreateVersion7(), Agora);
        internos.Atualizar(proprietario, endereco, null, Agora);
        return internos;
    }

    [Fact]
    public void Ausente_nao_mexe_no_que_esta_gravado()
    {
        var internos = Com("Maria", "Rua A, 100");
        internos.Atualizar(null, null, null, Agora);
        Assert.Equal("Maria", internos.ProprietarioNome);
        Assert.Equal("Rua A, 100", internos.Endereco);
    }

    [Fact]
    public void Proprietario_vazio_apaga_e_texto_vem_sem_espacos_nas_pontas()
    {
        var internos = Com(" Maria ", " Rua A, 100 ");
        Assert.Equal("Maria", internos.ProprietarioNome);
        Assert.Equal("Rua A, 100", internos.Endereco);

        internos.Atualizar("   ", "", null, Agora);
        Assert.Null(internos.ProprietarioNome);
        Assert.Equal("", internos.Endereco);
    }

    [Fact]
    public void Registra_o_operador_so_quando_informado()
    {
        var internos = Com("Maria", "");
        Assert.Null(internos.AtualizadoPor);

        internos.Atualizar(null, null, "Bruna", Agora);
        Assert.Equal("Bruna", internos.AtualizadoPor);

        internos.Atualizar("Joana", null, null, Agora);
        Assert.Equal("Bruna", internos.AtualizadoPor);
    }

    [Fact]
    public void Copia_leva_o_endereco_mas_nao_o_proprietario()
    {
        var copia = Com("Maria", "Rua A, 100").CopiarPara(Guid.CreateVersion7(), Agora);
        Assert.Equal("Rua A, 100", copia.Endereco);
        Assert.Null(copia.ProprietarioNome);
    }
}
