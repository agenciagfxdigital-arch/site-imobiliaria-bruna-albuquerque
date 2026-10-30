using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Regras;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Telefone autorizado a usar o assistente do WhatsApp (a Bruna e quem ela autorizar).</summary>
public sealed class Operador
{
    public Guid Id { get; private set; }
    /// <summary>DDI + DDD + número, só dígitos, com o nono dígito.</summary>
    public string Telefone { get; private set; } = "";
    public string Nome { get; private set; } = "";
    public bool Ativo { get; private set; }
    /// <summary>Imóvel em que a conversa está (ex.: fotos enviadas em seguida vão para ele).</summary>
    public Guid? ImovelAtualId { get; private set; }
    /// <summary>Quem está processando as mensagens deste operador agora (uma execução por vez).</summary>
    public string? ProcessandoToken { get; private set; }
    public DateTimeOffset? ProcessandoAte { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private Operador() { } // EF

    public Operador(string telefone, string nome, DateTimeOffset agora)
    {
        Id = Guid.CreateVersion7();
        Telefone = Regras.Telefone.Normalizar(telefone);
        Renomear(nome);
        Ativo = true;
        CriadoEm = agora;
    }

    /// <summary>Confere o operador encontrado pelo telefone: precisa existir e estar ativo.</summary>
    public static Operador GarantirAutorizado(Operador? operador) =>
        operador is { Ativo: true }
            ? operador
            : throw new DomainException(CodigoErro.NaoAutorizado, "Este telefone não está autorizado a usar o assistente.");

    public void Renomear(string nome)
    {
        var limpo = nome.Trim();
        if (limpo.Length is < 2 or > 80) throw DomainException.Validacao("O nome do operador deve ter de 2 a 80 letras.");
        Nome = limpo;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    public void DefinirImovelAtual(Guid? imovelId) => ImovelAtualId = imovelId;

    /// <summary>
    /// Livre, com a reserva vencida ou já reservado com o mesmo token (renovação). O Dados aplica esta mesma condição
    /// num UPDATE atômico, para duas execuções simultâneas não reservarem juntas.
    /// </summary>
    public bool PodeSerReservadoPor(string token, DateTimeOffset agora) =>
        ProcessandoAte is null || ProcessandoAte < agora || ProcessandoToken == token;

    public bool Reservar(string token, DateTimeOffset ate, DateTimeOffset agora)
    {
        if (!PodeSerReservadoPor(token, agora)) return false;
        ProcessandoToken = token;
        ProcessandoAte = ate;
        return true;
    }

    public void Liberar(string token)
    {
        if (ProcessandoToken != token) return;
        ProcessandoToken = null;
        ProcessandoAte = null;
    }
}
