using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Bairro de Videira usado nos filtros e no endereço aproximado dos imóveis.</summary>
public sealed class Bairro
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Slug { get; private set; } = "";

    private Bairro() { } // EF

    public Bairro(string nome)
    {
        Id = Guid.CreateVersion7();
        Renomear(nome);
    }

    public void Renomear(string nome)
    {
        var limpo = nome.Trim();
        if (limpo.Length is < 2 or > 60) throw DomainException.Validacao("O nome do bairro deve ter de 2 a 60 letras.");
        Nome = limpo;
        Slug = Texto.Slug(limpo);
    }

    /// <summary>Mesmo bairro, escrito com ou sem acento e maiúsculas (ex.: "centro" = "Centro").</summary>
    public bool TemONome(string nome) => Texto.Normalizar(Nome) == Texto.Normalizar(nome);
}
