using GFX.SiteBruna.Modelo.Valores;

namespace GFX.SiteBruna.Aplicacao.DTOs.Whatsapp;

/// <summary>O que teria sido enviado pelo WhatsApp no modo simulado.</summary>
public sealed record MensagemSimulada(string Telefone, string Texto, IReadOnlyList<BotaoDoWhatsapp> Botoes);
