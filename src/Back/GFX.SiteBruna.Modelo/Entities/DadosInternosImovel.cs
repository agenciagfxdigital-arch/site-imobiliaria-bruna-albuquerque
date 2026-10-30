using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>
/// Dados do imóvel que só o painel vê: o endereço exato nunca aparece no site, e quem alterou só interessa à Bruna.
/// </summary>
public sealed class DadosInternosImovel
{
    public const int TamanhoMaximoProprietario = 120;
    public const int TamanhoMaximoEndereco = 300;

    public Guid ImovelId { get; private set; }
    public string? ProprietarioNome { get; private set; }
    public string Endereco { get; private set; } = "";
    /// <summary>Operador do WhatsApp que alterou por último.</summary>
    public string? AtualizadoPor { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private DadosInternosImovel() { } // EF

    public DadosInternosImovel(Guid imovelId, DateTimeOffset agora)
    {
        ImovelId = imovelId;
        AtualizadoEm = agora;
    }

    /// <summary>
    /// Grava só o que veio: <c>null</c> não mexe; proprietário vazio apaga. O painel não informa
    /// <paramref name="autor"/> e por isso não troca <see cref="AtualizadoPor"/>.
    /// </summary>
    public void Atualizar(string? proprietarioNome, string? endereco, string? autor, DateTimeOffset agora)
    {
        ValidarTamanhos(proprietarioNome, endereco);
        if (proprietarioNome is not null) ProprietarioNome = proprietarioNome.Trim() is { Length: > 0 } nome ? nome : null;
        if (endereco is not null) Endereco = endereco.Trim();
        if (!string.IsNullOrWhiteSpace(autor)) AtualizadoPor = autor.Trim();
        AtualizadoEm = agora;
    }

    /// <summary>Na cópia do imóvel vai o endereço (mesmo prédio, outra unidade), mas não o proprietário.</summary>
    public DadosInternosImovel CopiarPara(Guid imovelId, DateTimeOffset agora) =>
        new(imovelId, agora) { Endereco = Endereco };

    internal static void ValidarTamanhos(string? proprietarioNome, string? endereco)
    {
        if (proprietarioNome?.Trim().Length > TamanhoMaximoProprietario)
            throw DomainException.Validacao($"O nome do proprietário pode ter até {TamanhoMaximoProprietario} letras.");
        if (endereco?.Trim().Length > TamanhoMaximoEndereco)
            throw DomainException.Validacao($"O endereço pode ter até {TamanhoMaximoEndereco} letras.");
    }
}
