namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

public sealed record DepoimentoResponse(Guid Id, string Nome, string Texto, string? FotoUrl, bool ExibirNaHome, int Ordem);
