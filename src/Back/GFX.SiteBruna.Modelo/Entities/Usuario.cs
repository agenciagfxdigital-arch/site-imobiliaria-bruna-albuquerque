using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Interfaces;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Quem entra no painel (a Bruna e quem ela autorizar). Todo usuário é administrador do site.</summary>
public sealed partial class Usuario
{
    public const int TamanhoMinimoSenha = 12;

    public Guid Id { get; private set; }
    /// <summary>Sempre em minúsculas.</summary>
    public string Email { get; private set; } = "";
    public string Nome { get; private set; } = "";
    /// <summary>A senha só existe como hash.</summary>
    public string HashDaSenha { get; private set; } = "";
    public bool Ativo { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset? UltimoAcessoEm { get; private set; }

    private Usuario() { } // EF

    public Usuario(string email, string nome, string senha, IHasherSenha hasher, DateTimeOffset agora)
    {
        Id = Guid.CreateVersion7();
        Email = NormalizarEmail(email);
        Renomear(nome);
        ValidarNovaSenha(senha);
        HashDaSenha = hasher.Gerar(senha);
        Ativo = true;
        CriadoEm = agora;
    }

    public static string NormalizarEmail(string? email)
    {
        var e = (email ?? "").Trim().ToLowerInvariant();
        if (e.Length > 200 || !EmailValido().IsMatch(e)) throw DomainException.Validacao("E-mail inválido.");
        return e;
    }

    public void Renomear(string nome)
    {
        var n = (nome ?? "").Trim();
        if (n.Length is < 2 or > 80) throw DomainException.Validacao("O nome deve ter de 2 a 80 letras.");
        Nome = n;
    }

    /// <summary>Mínimo de 12 caracteres, com letra maiúscula, minúscula, número e símbolo (mesma regra do sistema interno).</summary>
    public static void ValidarNovaSenha(string? senha)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < TamanhoMinimoSenha || !senha.Any(char.IsUpper)
            || !senha.Any(char.IsLower) || !senha.Any(char.IsDigit) || senha.All(char.IsLetterOrDigit))
            throw DomainException.Validacao(
                $"A senha precisa ter no mínimo {TamanhoMinimoSenha} caracteres, com letra maiúscula, minúscula, número e símbolo.");
    }

    /// <summary>
    /// Login: confere a senha de quem existe e está ativo. Sem usuário, gasta o mesmo tempo (não revela quais e-mails
    /// existem). Quando o hash é antigo, grava um novo. A mensagem de erro é sempre a mesma.
    /// </summary>
    public static Usuario Entrar(Usuario? usuario, string senha, IHasherSenha hasher, DateTimeOffset agora)
    {
        var erro = new DomainException(CodigoErro.NaoAutorizado, "E-mail ou senha incorretos.");
        if (usuario is null)
        {
            hasher.SimularVerificacao(senha);
            throw erro;
        }
        var verificacao = hasher.Verificar(usuario.HashDaSenha, senha);
        if (verificacao == VerificacaoSenha.Falhou || !usuario.Ativo) throw erro;
        if (verificacao == VerificacaoSenha.ConfereComHashAntigo) usuario.HashDaSenha = hasher.Gerar(senha);
        usuario.UltimoAcessoEm = agora;
        return usuario;
    }

    public void TrocarSenha(string senhaAtual, string novaSenha, IHasherSenha hasher)
    {
        if (hasher.Verificar(HashDaSenha, senhaAtual) == VerificacaoSenha.Falhou)
            throw DomainException.Validacao("A senha atual não confere.");
        ValidarNovaSenha(novaSenha);
        if (novaSenha == senhaAtual) throw DomainException.Validacao("A nova senha precisa ser diferente da atual.");
        HashDaSenha = hasher.Gerar(novaSenha);
    }

    /// <summary>Senha nova definida por outro administrador (ex.: esqueceu a senha).</summary>
    public void RedefinirSenha(string novaSenha, IHasherSenha hasher)
    {
        ValidarNovaSenha(novaSenha);
        HashDaSenha = hasher.Gerar(novaSenha);
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}
