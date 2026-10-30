using GFX.SiteBruna.Modelo.Exceptions;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>
/// Depoimento de cliente. Publicar só com autorização do cliente (LGPD): de preferência só o primeiro nome e foto
/// apenas se ele permitiu.
/// </summary>
public sealed class Depoimento
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Texto { get; private set; } = "";
    public string? FotoUrl { get; private set; }
    public bool ExibirNaHome { get; private set; }
    public int Ordem { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private Depoimento() { } // EF

    public Depoimento(string nome, string texto, string? fotoUrl, bool exibirNaHome, int ordem, DateTimeOffset agora)
    {
        Id = Guid.CreateVersion7();
        CriadoEm = agora;
        Atualizar(nome, texto, fotoUrl, exibirNaHome, ordem);
    }

    public void Atualizar(string nome, string texto, string? fotoUrl, bool exibirNaHome, int ordem)
    {
        var n = (nome ?? "").Trim();
        var t = (texto ?? "").Trim();
        var foto = string.IsNullOrWhiteSpace(fotoUrl) ? null : fotoUrl.Trim();
        if (n.Length < 2) throw DomainException.Validacao("Informe o nome.");
        if (n.Length > 120) throw DomainException.Validacao("O nome pode ter até 120 letras.");
        if (t.Length < 5) throw DomainException.Validacao("Escreva o depoimento.");
        if (t.Length > 2000) throw DomainException.Validacao("O depoimento pode ter até 2000 letras.");
        if (foto?.Length > 1000) throw DomainException.Validacao("O endereço da foto é longo demais.");
        if (ordem is < 0 or > 999) throw DomainException.Validacao("A ordem deve ficar entre 0 e 999.");
        Nome = n;
        Texto = t;
        FotoUrl = foto;
        ExibirNaHome = exibirNaHome;
        Ordem = ordem;
    }
}
