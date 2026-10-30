using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Integracao;

/// <param name="ImovelAtual">Imóvel em que a conversa está (recebe as próximas fotos).</param>
internal sealed record SessaoDoOperador(Imovel? ImovelAtual, int TotalDeFotos, IReadOnlyList<AcaoPendente> Pendentes);
