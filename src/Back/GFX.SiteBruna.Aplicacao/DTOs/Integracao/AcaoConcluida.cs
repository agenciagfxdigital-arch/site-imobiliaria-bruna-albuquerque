using GFX.SiteBruna.Modelo.Enums;

namespace GFX.SiteBruna.Aplicacao.DTOs.Integracao;

/// <param name="LinkPublico">Página do imóvel no site, quando ele está visível.</param>
internal sealed record AcaoConcluida(TipoAcao Tipo, string Codigo, string Mensagem, string? LinkPublico);
