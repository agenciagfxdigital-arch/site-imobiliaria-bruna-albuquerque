using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class BairroConfig : IEntityTypeConfiguration<Bairro>
{
    public void Configure(EntityTypeBuilder<Bairro> b)
    {
        b.ToTable("neighborhoods");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nome).HasMaxLength(60);
        b.Property(x => x.Slug).HasMaxLength(80);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
