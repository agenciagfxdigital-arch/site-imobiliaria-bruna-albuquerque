using System.Text.RegularExpressions;
using GFX.SiteBruna.Modelo.Exceptions;
using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Modelo.Entities;

/// <summary>Configurações do site (linha única): contato, CRECI, selo de vendido e IDs de GA4/Meta Pixel.</summary>
public sealed partial class Configuracoes
{
    public const int IdUnico = 1;

    public int Id { get; private set; } = IdUnico;
    /// <summary>DDI + DDD + número, só dígitos. Todo botão de WhatsApp do site usa este número.</summary>
    public string Whatsapp { get; private set; } = "5549999304262";
    public string MensagemPadrao { get; private set; } = "Olá, Bruna! Vim pelo site e gostaria de conversar sobre imóveis.";
    public string Instagram { get; private set; } = "";
    public string Facebook { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string Creci { get; private set; } = "CRECI/SC 60723";
    public string Cidade { get; private set; } = "Videira/SC";
    /// <summary>Só é carregado depois do consentimento de cookies.</summary>
    public string Ga4Id { get; private set; } = "";
    /// <summary>Só é carregado depois do consentimento de cookies.</summary>
    public string MetaPixelId { get; private set; } = "";
    /// <summary>Por quantos dias o imóvel vendido/alugado continua no site com o selo.</summary>
    public int DiasSeloVendido { get; private set; } = 30;
    public DateTimeOffset AtualizadoEm { get; private set; }

    public void Atualizar(DadosDasConfiguracoes d, DateTimeOffset agora)
    {
        var whatsapp = NaoDigito().Replace(d.Whatsapp ?? "", "");
        if (whatsapp.Length is < 12 or > 13)
            throw DomainException.Validacao("WhatsApp: use DDI + DDD + número, ex.: 55 49 99999-9999.");
        var mensagem = (d.MensagemPadrao ?? "").Trim();
        if (mensagem.Length is < 5 or > 500) throw DomainException.Validacao("A mensagem padrão do WhatsApp deve ter de 5 a 500 letras.");
        var email = (d.Email ?? "").Trim();
        if (email.Length > 0 && (email.Length > 200 || !EmailValido().IsMatch(email))) throw DomainException.Validacao("E-mail inválido.");
        var cidade = (d.Cidade ?? "").Trim();
        if (cidade.Length is < 2 or > 60) throw DomainException.Validacao("Informe a cidade.");
        var ga4 = (d.Ga4Id ?? "").Trim().ToUpperInvariant();
        if (ga4.Length > 0 && !Ga4Valido().IsMatch(ga4)) throw DomainException.Validacao("O ID do GA4 deve começar com G-.");
        var pixel = (d.MetaPixelId ?? "").Trim();
        if (pixel.Length > 0 && !PixelValido().IsMatch(pixel)) throw DomainException.Validacao("O ID do Pixel deve ter só números.");
        if (d.DiasSeloVendido is < 0 or > 365) throw DomainException.Validacao("O selo de vendido pode durar de 0 a 365 dias.");
        Limite(d.Instagram, 200, "O Instagram");
        Limite(d.Facebook, 300, "O Facebook");
        Limite(d.Creci, 60, "O CRECI");

        Whatsapp = whatsapp;
        MensagemPadrao = mensagem;
        Instagram = d.Instagram.Trim();
        Facebook = d.Facebook.Trim();
        Email = email;
        Creci = d.Creci.Trim();
        Cidade = cidade;
        Ga4Id = ga4;
        MetaPixelId = pixel;
        DiasSeloVendido = d.DiasSeloVendido;
        AtualizadoEm = agora;
    }

    private static void Limite(string? valor, int maximo, string nome)
    {
        if ((valor ?? "").Trim().Length > maximo) throw DomainException.Validacao($"{nome} pode ter até {maximo} letras.");
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigito();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();

    [GeneratedRegex("^G-[A-Z0-9]+$")]
    private static partial Regex Ga4Valido();

    [GeneratedRegex(@"^\d{6,20}$")]
    private static partial Regex PixelValido();
}
