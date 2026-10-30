namespace GFX.SiteBruna.Modelo.Valores;

/// <param name="Codigo">Presente quando a ficha é para atualizar um imóvel que já existe.</param>
/// <param name="Campos">Campos preenchidos (o bairro vem à parte, pelo nome).</param>
/// <param name="Bairro">Nome do bairro como foi escrito; a Aplicacao confere com o cadastro.</param>
/// <param name="Problemas">Todos os problemas encontrados, para a Bruna corrigir de uma vez.</param>
public sealed record FichaLida(string? Codigo, CamposDoImovel Campos, string? Bairro, IReadOnlyList<string> Problemas);
