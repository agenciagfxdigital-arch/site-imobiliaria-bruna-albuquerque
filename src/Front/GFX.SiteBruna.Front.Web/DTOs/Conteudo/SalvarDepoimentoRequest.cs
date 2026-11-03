namespace GFX.SiteBruna.Front.Web.DTOs.Conteudo;

/// <param name="Id">Vazio = depoimento novo.</param>
public sealed record SalvarDepoimentoRequest(Guid? Id, string Nome, string Texto, string? FotoUrl, bool ExibirNaHome, int Ordem);
