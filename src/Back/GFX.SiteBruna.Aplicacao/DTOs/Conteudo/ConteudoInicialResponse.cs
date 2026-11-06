namespace GFX.SiteBruna.Aplicacao.DTOs.Conteudo;

/// <summary>Quantos registros foram incluídos (0 quando a lista já tinha cadastro).</summary>
public sealed record ConteudoInicialResponse(int Bairros, int Perguntas, int Depoimentos);
