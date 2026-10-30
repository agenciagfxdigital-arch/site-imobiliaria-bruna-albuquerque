using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class ConfiguracoesConfig : IEntityTypeConfiguration<Configuracoes>
{
    public void Configure(EntityTypeBuilder<Configuracoes> b)
    {
        b.ToTable("settings", t =>
        {
            t.HasCheckConstraint("ck_settings_linha_unica", $"id = {Configuracoes.IdUnico}");
            t.HasCheckConstraint("ck_settings_dias_selo_vendido", "dias_selo_vendido between 0 and 365");
        });
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Whatsapp).HasMaxLength(13);
        b.Property(c => c.MensagemPadrao).HasMaxLength(500);
        b.Property(c => c.Instagram).HasMaxLength(200);
        b.Property(c => c.Facebook).HasMaxLength(300);
        b.Property(c => c.Email).HasMaxLength(200);
        b.Property(c => c.Creci).HasMaxLength(60);
        b.Property(c => c.Cidade).HasMaxLength(60);
        b.Property(c => c.Ga4Id).HasColumnName("ga4_id").HasMaxLength(40);
        b.Property(c => c.MetaPixelId).HasMaxLength(20);
    }
}
