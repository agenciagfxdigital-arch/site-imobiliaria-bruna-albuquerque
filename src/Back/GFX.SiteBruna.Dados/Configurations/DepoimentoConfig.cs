using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class DepoimentoConfig : IEntityTypeConfiguration<Depoimento>
{
    public void Configure(EntityTypeBuilder<Depoimento> b)
    {
        b.ToTable("testimonials");
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).ValueGeneratedNever();
        b.Property(d => d.Nome).HasMaxLength(120);
        b.Property(d => d.Texto).HasMaxLength(2000);
        b.Property(d => d.FotoUrl).HasMaxLength(1000);
        b.Property(d => d.ExibirNaHome).HasColumnName("exibir_home");
        b.HasIndex(d => d.Ordem);
    }
}
