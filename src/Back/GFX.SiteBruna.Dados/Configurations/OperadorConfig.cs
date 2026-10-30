using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class OperadorConfig : IEntityTypeConfiguration<Operador>
{
    public void Configure(EntityTypeBuilder<Operador> b)
    {
        b.ToTable("agent_operators", t => t.HasCheckConstraint("ck_agent_operators_phone", "phone ~ '^[0-9]{12,13}$'"));
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).ValueGeneratedNever();
        b.Property(o => o.Telefone).HasColumnName("phone").HasMaxLength(13);
        b.HasIndex(o => o.Telefone).IsUnique();
        b.Property(o => o.Nome).HasMaxLength(80);
        b.Property(o => o.ImovelAtualId).HasColumnName("current_property_id");
        b.Property(o => o.ProcessandoToken).HasMaxLength(64);
        b.HasOne<Imovel>().WithMany().HasForeignKey(o => o.ImovelAtualId).OnDelete(DeleteBehavior.SetNull);
    }
}
