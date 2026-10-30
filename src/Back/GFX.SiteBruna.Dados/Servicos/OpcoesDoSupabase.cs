namespace GFX.SiteBruna.Dados.Servicos;

/// <summary>Seção <c>Supabase</c>: só o Storage (o banco vem de <c>ConnectionStrings:DefaultConnection</c>).</summary>
internal sealed class OpcoesDoSupabase
{
    public string Url { get; set; } = "";
    /// <summary>Chave service_role: só no servidor, nunca no front nem em arquivo versionado.</summary>
    public string ChaveServico { get; set; } = "";
    public string Bucket { get; set; } = "media";
}
