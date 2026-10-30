namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Foto de um imóvel, já processada (WebP, sem EXIF/GPS) e gravada no storage.</summary>
public sealed class FotoImovel
{
    public Guid Id { get; private set; }
    public Guid ImovelId { get; private set; }
    public string Url { get; private set; } = "";
    /// <summary>Caminho no storage; nulo em fotos antigas sem arquivo próprio. Cópias de imóvel compartilham o arquivo.</summary>
    public string? CaminhoNoStorage { get; private set; }
    public int Ordem { get; private set; }
    public bool Capa { get; private set; }

    private FotoImovel() { } // EF

    public FotoImovel(Guid imovelId, string url, string? caminhoNoStorage, int ordem, bool capa)
    {
        Id = Guid.CreateVersion7();
        ImovelId = imovelId;
        Url = url;
        CaminhoNoStorage = caminhoNoStorage;
        Posicionar(ordem, capa);
    }

    public void Posicionar(int ordem, bool capa)
    {
        Ordem = ordem;
        Capa = capa;
    }

    /// <summary>Na cópia do imóvel, a foto aponta para o mesmo arquivo (não duplica no storage).</summary>
    public FotoImovel CopiarPara(Guid imovelId) => new(imovelId, Url, CaminhoNoStorage, Ordem, Capa);
}
