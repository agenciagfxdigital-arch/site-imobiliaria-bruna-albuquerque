using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

/// <summary>Tudo aqui é público (aparece no site); não coloque segredo em Configurações.</summary>
public sealed record ConfiguracoesResponse(
    string Whatsapp, string MensagemPadrao, string Instagram, string Facebook, string Email, string Creci, string Cidade,
    string Ga4Id, string MetaPixelId, int DiasSeloVendido)
{
    public static ConfiguracoesResponse De(Configuracoes c) => new(
        c.Whatsapp, c.MensagemPadrao, c.Instagram, c.Facebook, c.Email, c.Creci, c.Cidade, c.Ga4Id, c.MetaPixelId, c.DiasSeloVendido);
}
