using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class FaqConfig : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> b)
    {
        b.ToTable("faqs");
        b.HasKey(f => f.Id);
        b.Property(f => f.Id).ValueGeneratedNever();
        b.Property(f => f.Pergunta).HasMaxLength(300);
        b.Property(f => f.Resposta).HasMaxLength(3000);
        b.HasIndex(f => f.Ordem);
    }
}
