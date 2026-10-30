using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>
/// Textos e fotos editáveis das páginas (linha única). Enquanto a Bruna não salvar uma página, o site usa os textos
/// padrão (<see cref="ConteudoHome.Padrao"/> etc.).
/// </summary>
public sealed class Paginas
{
    public const int IdUnico = 1;

    public int Id { get; private set; } = IdUnico;
    public ConteudoHome Home { get; private set; } = ConteudoHome.Padrao;
    public ConteudoSobre Sobre { get; private set; } = ConteudoSobre.Padrao;
    public ConteudoMcmv Mcmv { get; private set; } = ConteudoMcmv.Padrao;
    public DateTimeOffset AtualizadoEm { get; private set; }

    public void AtualizarHome(ConteudoHome conteudo, DateTimeOffset agora)
    {
        conteudo.Validar();
        Home = conteudo;
        AtualizadoEm = agora;
    }

    public void AtualizarSobre(ConteudoSobre conteudo, DateTimeOffset agora)
    {
        conteudo.Validar();
        Sobre = conteudo;
        AtualizadoEm = agora;
    }

    public void AtualizarMcmv(ConteudoMcmv conteudo, DateTimeOffset agora)
    {
        conteudo.Validar();
        Mcmv = conteudo;
        AtualizadoEm = agora;
    }
}
