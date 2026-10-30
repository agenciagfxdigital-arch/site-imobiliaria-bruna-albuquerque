using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class AcaoPendenteConfig : IEntityTypeConfiguration<AcaoPendente>
{
    public void Configure(EntityTypeBuilder<AcaoPendente> b)
    {
        b.ToTable("agent_pending_actions", t =>
        {
            t.HasCheckConstraint("ck_agent_pending_actions_tipo", $"tipo in ({EnumComoTexto<TipoAcao>.ListaSql})");
            t.HasCheckConstraint("ck_agent_pending_actions_status", $"status in ({EnumComoTexto<StatusAcao>.ListaSql})");
        });
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).ValueGeneratedNever();
        b.Property(a => a.OperadorId).HasColumnName("operator_id");
        b.Property(a => a.ImovelId).HasColumnName("property_id");
        b.Property(a => a.Tipo).HasConversion(new EnumComoTexto<TipoAcao>());
        b.Property(a => a.Status).HasConversion(new EnumComoTexto<StatusAcao>());
        b.Property(a => a.NovoStatus).HasConversion(new EnumComoTexto<StatusImovel>());
        b.Property(a => a.DadosPropostos).HasColumnType("jsonb").HasConversion(Json.Conversor<DadosDoImovel?>());
        b.Property(a => a.Diferencas).HasColumnName("diff").HasColumnType("jsonb").HasConversion(Json.Conversor<List<MudancaCampo>>());
        b.HasOne<Operador>().WithMany().HasForeignKey(a => a.OperadorId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Imovel>().WithMany().HasForeignKey(a => a.ImovelId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(a => new { a.OperadorId, a.Status, a.CriadoEm });
    }
}
