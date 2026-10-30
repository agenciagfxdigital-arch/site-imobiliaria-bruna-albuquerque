using GFX.SiteBruna.Aplicacao.Interfaces;
using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Repositories;

internal sealed class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    public Task<Usuario?> GetByEmail(string email) => context.Usuario.FirstOrDefaultAsync(u => u.Email == email);

    public Task<Usuario?> GetById(Guid id) => context.Usuario.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<IReadOnlyList<Usuario>> GetAll() => await context.Usuario.AsNoTracking().OrderBy(u => u.Nome).ToListAsync();

    public void Add(Usuario usuario) => context.Usuario.Add(usuario);
}
