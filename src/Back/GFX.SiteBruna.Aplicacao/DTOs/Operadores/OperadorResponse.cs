using GFX.SiteBruna.Modelo.Entities;

namespace GFX.SiteBruna.Aplicacao.DTOs.Operadores;

public sealed record OperadorResponse(Guid Id, string Telefone, string Nome, bool Ativo)
{
    public static OperadorResponse De(Operador o) => new(o.Id, o.Telefone, o.Nome, o.Ativo);
}
