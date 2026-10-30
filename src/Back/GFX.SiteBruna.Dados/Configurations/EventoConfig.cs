using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

/// <summary>Métricas internas, sem dados pessoais nem IP. Uma visualização por visitante por imóvel.</summary>
internal sealed class EventoConfig : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> b)
    {
        b.ToTable("events", t =>
        {
            t.HasCheckConstraint("ck_events_tipo", $"tipo in ({EnumComoTexto<TipoEvento>.ListaSql})");
            t.HasCheckConstraint("ck_events_dispositivo", $"dispositivo in ({EnumComoTexto<Dispositivo>.ListaSql})");
            t.HasCheckConstraint("ck_events_visitor_id", "visitor_id ~ '^[A-Za-z0-9-]{8,64}$'");
        });
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Tipo).HasConversion(new EnumComoTexto<TipoEvento>());
        b.Property(e => e.Dispositivo).HasConversion(new EnumComoTexto<Dispositivo>());
        b.Property(e => e.ImovelId).HasColumnName("property_id");
        b.Property(e => e.VisitanteId).HasColumnName("visitor_id").HasMaxLength(64);
        b.Property(e => e.Pagina).HasMaxLength(500);
        b.Property(e => e.UtmSource).HasMaxLength(120);
        b.Property(e => e.UtmMedium).HasMaxLength(120);
        b.Property(e => e.UtmCampaign).HasMaxLength(200);
        b.Property(e => e.Referrer).HasMaxLength(200);
        b.Property(e => e.Detalhes).HasColumnName("meta").HasColumnType("jsonb")
            .HasConversion(Json.Conversor<Dictionary<string, string?>?>());
        b.HasOne<Imovel>().WithMany().HasForeignKey(e => e.ImovelId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(e => e.CriadoEm);
        b.HasIndex(e => new { e.Tipo, e.CriadoEm });
        b.HasIndex(e => new { e.ImovelId, e.VisitanteId }).IsUnique().HasFilter("tipo = 'property_view'");
    }
}
