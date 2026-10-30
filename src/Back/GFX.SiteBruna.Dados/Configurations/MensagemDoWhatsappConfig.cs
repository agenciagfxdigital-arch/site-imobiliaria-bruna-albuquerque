using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

/// <summary>Retenção sugerida: 30 dias (as mensagens só servem para suporte e para não processar reenvios).</summary>
internal sealed class MensagemDoWhatsappConfig : IEntityTypeConfiguration<MensagemDoWhatsapp>
{
    public void Configure(EntityTypeBuilder<MensagemDoWhatsapp> b)
    {
        b.ToTable("whatsapp_mensagens",
            t => t.HasCheckConstraint("ck_whatsapp_mensagens_status", $"status in ({EnumComoTexto<StatusMensagem>.ListaSql})"));
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).ValueGeneratedNever();
        b.Property(m => m.Wamid).HasMaxLength(200);
        b.HasIndex(m => m.Wamid).IsUnique();
        b.Property(m => m.OperadorId).HasColumnName("operator_id");
        b.Property(m => m.Telefone).HasMaxLength(20);
        b.Property(m => m.Status).HasConversion(new EnumComoTexto<StatusMensagem>());
        b.Property(m => m.Erro).HasMaxLength(2000);
        b.Property(m => m.Conteudo).HasColumnType("jsonb").HasConversion(Json.Conversor<ConteudoMensagem>());
        b.Ignore(m => m.EhFoto);
        b.HasOne<Operador>().WithMany().HasForeignKey(m => m.OperadorId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(m => new { m.OperadorId, m.RecebidaEm }).HasFilter("status = 'pendente'");
    }
}
