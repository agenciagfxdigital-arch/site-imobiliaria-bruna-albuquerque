using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Modelo.Valores;

/// <summary>Para onde vai a imagem: fotos de imóvel ficam numa pasta por imóvel; as demais, numa pasta do site.</summary>
/// <param name="ImovelId">Preenchido quando a imagem é de um imóvel.</param>
/// <param name="Pasta">Preenchida quando a imagem é do site (foto da Bruna, depoimento, entrega de chave).</param>
public sealed record DestinoImagem(Guid? ImovelId, PastaDoSite? Pasta)
{
    public static DestinoImagem DoImovel(Guid imovelId) => new(imovelId, null);

    public static DestinoImagem DoSite(PastaDoSite pasta) => new(null, pasta);

    /// <summary>Pasta no storage: <c>imoveis/&lt;id&gt;</c> ou o nome da pasta do site.</summary>
    public string NomeDaPasta => ImovelId is { } id ? $"imoveis/{id:D}" : Pasta!.Value.ToString().ToLowerInvariant();
}
