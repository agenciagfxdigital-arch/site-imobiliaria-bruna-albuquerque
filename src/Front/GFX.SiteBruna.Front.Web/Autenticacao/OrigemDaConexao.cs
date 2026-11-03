namespace GFX.SiteBruna.Front.Web.Autenticacao;

/// <summary>
/// IP de quem abriu o painel, para o limite de tentativas de login da Api valer por pessoa. No Blazor Server a requisição
/// HTTP só existe na abertura da página: o <c>App.razor</c> lê o IP ali e o <c>Routes</c> o guarda aqui, no escopo do
/// circuito (mesma solução do sistema interno).
/// </summary>
public sealed class OrigemDaConexao
{
    public string? Ip { get; private set; }

    public void Definir(string? ip)
    {
        if (!string.IsNullOrWhiteSpace(ip)) Ip = ip;
    }
}
