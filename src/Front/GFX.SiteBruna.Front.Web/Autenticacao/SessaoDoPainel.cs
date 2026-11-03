using System.Security.Cryptography;
using System.Text.Json;
using GFX.SiteBruna.Front.Web.DTOs.Usuarios;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace GFX.SiteBruna.Front.Web.Autenticacao;

/// <summary>
/// Quem está logado no painel. A sessão vem da resposta do login e fica no localStorage do navegador, criptografada pelo
/// Data Protection do servidor: o navegador guarda, mas não lê nem altera o token. Por isso o painel é interativo e sem
/// pré-renderização (o armazenamento só existe com o circuito ligado). Um por circuito (aba).
/// </summary>
public sealed class SessaoDoPainel(ProtectedLocalStorage armazenamento)
{
    private const string Chave = "ba-sessao-painel";

    private SessaoSalva? _sessao;
    private bool _carregada;

    /// <summary>Avisa o layout quando alguém entra ou sai (ou o token vence).</summary>
    public event Action? Mudou;

    /// <summary>Sessão válida (não vencida) ou <c>null</c>. Usada pelos clientes da Api para anexar o token.</summary>
    public async Task<SessaoSalva?> ObterSessao()
    {
        if (!_carregada)
        {
            _sessao = await Ler();
            _carregada = true;
        }
        if (_sessao is { Expirada: true }) await Sair();
        return _sessao;
    }

    public async Task Entrar(SessaoResponse resposta)
    {
        _sessao = SessaoSalva.De(resposta);
        _carregada = true;
        await armazenamento.SetAsync(Chave, _sessao);
        Mudou?.Invoke();
    }

    public async Task Sair()
    {
        var tinhaSessao = _sessao is not null;
        _sessao = null;
        _carregada = true;
        try
        {
            await armazenamento.DeleteAsync(Chave);
        }
        catch (InvalidOperationException)
        {
            // Sem circuito (ex.: a tela já foi fechada): não há o que apagar agora
        }
        if (tinhaSessao) Mudou?.Invoke();
    }

    private async Task<SessaoSalva?> Ler()
    {
        try
        {
            var resultado = await armazenamento.GetAsync<SessaoSalva>(Chave);
            return resultado.Success ? resultado.Value : null;
        }
        catch (Exception e) when (e is CryptographicException or JsonException)
        {
            // Chaves do Data Protection mudaram ou o valor foi adulterado: descarta e pede o login de novo
            await armazenamento.DeleteAsync(Chave);
            return null;
        }
    }
}
