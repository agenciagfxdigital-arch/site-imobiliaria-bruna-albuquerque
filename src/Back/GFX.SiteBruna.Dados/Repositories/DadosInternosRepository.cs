using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class DadosInternosRepository(AppDbContext context) : IDadosInternosRepository
{
    /// <summary>Procura primeiro o que foi incluído e ainda não gravado (o mesmo imóvel pode ser salvo duas vezes no caso de uso).</summary>
    public async Task<DadosInternosImovel?> GetByImovel(Guid imovelId) =>
        context.DadosInternosImovel.Local.FirstOrDefault(d => d.ImovelId == imovelId)
        ?? await context.DadosInternosImovel.FirstOrDefaultAsync(d => d.ImovelId == imovelId);

    public void Add(DadosInternosImovel dados) => context.DadosInternosImovel.Add(dados);
}
