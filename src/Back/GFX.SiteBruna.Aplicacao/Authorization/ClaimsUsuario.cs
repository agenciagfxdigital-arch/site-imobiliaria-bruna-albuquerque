namespace GFX.SiteBruna.Aplicacao.Authorization;

/// <summary>Nomes das claims do token do painel (o Dados gera, a Api lê).</summary>
public static class ClaimsUsuario
{
    public const string Id = "sub";
    public const string Nome = "name";
    public const string Email = "email";
}
