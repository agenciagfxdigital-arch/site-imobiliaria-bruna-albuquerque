using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Modelo.Interfaces;

/// <summary>
/// O algoritmo de hash é detalhe técnico (implementado no Dados); o domínio só decide quando e como a senha é conferida.
/// </summary>
public interface IHasherSenha
{
    string Gerar(string senha);

    VerificacaoSenha Verificar(string hash, string senha);

    /// <summary>Gasta o mesmo tempo de uma verificação real, para o tempo de resposta não revelar se a conta existe.</summary>
    void SimularVerificacao(string senha);
}
