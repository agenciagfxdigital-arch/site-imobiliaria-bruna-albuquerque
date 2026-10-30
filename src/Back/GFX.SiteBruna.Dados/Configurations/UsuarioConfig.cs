using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).ValueGeneratedNever();
        b.Property(u => u.Email).HasMaxLength(200);
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.Nome).HasMaxLength(80);
        b.Property(u => u.HashDaSenha).HasMaxLength(500);
    }
}
