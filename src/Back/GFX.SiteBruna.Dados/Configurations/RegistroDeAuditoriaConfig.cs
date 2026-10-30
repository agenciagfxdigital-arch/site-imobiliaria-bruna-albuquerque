using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

/// <summary>A auditoria sobrevive ao imóvel e ao operador (as referências viram nulas).</summary>
internal sealed class RegistroDeAuditoriaConfig : IEntityTypeConfiguration<RegistroDeAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroDeAuditoria> b)
    {
        b.ToTable("agent_actions");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.OperadorId).HasColumnName("operator_id");
        b.Property(r => r.ImovelId).HasColumnName("property_id");
        b.Property(r => r.Operacao).HasMaxLength(40);
        b.Property(r => r.Detalhe).HasMaxLength(500);
        b.Property(r => r.Erro).HasMaxLength(2000);
        b.Property(r => r.Antes).HasColumnType("jsonb").HasConversion(Json.Conversor<DadosDoImovel?>());
        b.Property(r => r.Depois).HasColumnType("jsonb").HasConversion(Json.Conversor<DadosDoImovel?>());
        b.HasOne<Operador>().WithMany().HasForeignKey(r => r.OperadorId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Imovel>().WithMany().HasForeignKey(r => r.ImovelId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(r => new { r.OperadorId, r.Operacao, r.CriadoEm });
    }
}
