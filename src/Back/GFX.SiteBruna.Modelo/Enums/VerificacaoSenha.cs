namespace GFX.SiteBruna.Modelo.Enums;

public enum VerificacaoSenha
{
    Falhou,
    Confere,
    /// <summary>Confere, mas o hash foi gerado com parâmetros antigos: gravar um hash novo.</summary>
    ConfereComHashAntigo,
}
