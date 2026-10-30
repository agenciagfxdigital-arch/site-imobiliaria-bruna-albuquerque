using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>PBKDF2 do ASP.NET Core Identity, atrás do contrato <see cref="IHasherSenha"/> do domínio (igual ao sistema interno).</summary>
internal sealed class HasherSenha : IHasherSenha
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object SemUsuario = new();
    private static readonly string HashFicticio = Hasher.HashPassword(SemUsuario, Guid.NewGuid().ToString());

    public string Gerar(string senha) => Hasher.HashPassword(SemUsuario, senha);

    public VerificacaoSenha Verificar(string hash, string senha) => Hasher.VerifyHashedPassword(SemUsuario, hash, senha) switch
    {
        PasswordVerificationResult.Success => VerificacaoSenha.Confere,
        PasswordVerificationResult.SuccessRehashNeeded => VerificacaoSenha.ConfereComHashAntigo,
        _ => VerificacaoSenha.Falhou,
    };

    public void SimularVerificacao(string senha) => Hasher.VerifyHashedPassword(SemUsuario, HashFicticio, senha);
}
