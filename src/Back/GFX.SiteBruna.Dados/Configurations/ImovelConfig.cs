using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class ImovelConfig : IEntityTypeConfiguration<Imovel>
{
    public void Configure(EntityTypeBuilder<Imovel> b)
    {
        b.ToTable("properties", t =>
        {
            t.HasCheckConstraint("ck_properties_codigo", "codigo ~ '^[A-Z0-9]{2,20}$'");
            t.HasCheckConstraint("ck_properties_finalidade", $"finalidade in ({EnumComoTexto<Finalidade>.ListaSql})");
            t.HasCheckConstraint("ck_properties_tipo", $"tipo in ({EnumComoTexto<TipoImovel>.ListaSql})");
            t.HasCheckConstraint("ck_properties_situacao", $"situacao in ({EnumComoTexto<Situacao>.ListaSql})");
            t.HasCheckConstraint("ck_properties_status", $"status in ({EnumComoTexto<StatusImovel>.ListaSql})");
            t.HasCheckConstraint("ck_properties_origem", $"origem in ({EnumComoTexto<OrigemImovel>.ListaSql})");
        });
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();
        b.Property(i => i.Codigo).HasMaxLength(20);
        b.HasIndex(i => i.Codigo).IsUnique();
        b.Property(i => i.Slug).HasMaxLength(80);
        b.Property(i => i.Titulo).HasMaxLength(Imovel.TamanhoMaximoTitulo);
        b.Property(i => i.Descricao).HasMaxLength(10_000);
        b.Property(i => i.Finalidade).HasConversion(new EnumComoTexto<Finalidade>());
        b.Property(i => i.Tipo).HasConversion(new EnumComoTexto<TipoImovel>());
        b.Property(i => i.Situacao).HasConversion(new EnumComoTexto<Situacao>());
        b.Property(i => i.Status).HasConversion(new EnumComoTexto<StatusImovel>());
        b.Property(i => i.Origem).HasConversion(new EnumComoTexto<OrigemImovel>());
        b.Property(i => i.Preco).HasPrecision(12, 2);
        b.Property(i => i.Aluguel).HasPrecision(10, 2);
        b.Property(i => i.Condominio).HasPrecision(10, 2);
        b.Property(i => i.Iptu).HasPrecision(10, 2);
        b.Property(i => i.EntradaAPartir).HasColumnName("entrada_a_partir").HasPrecision(12, 2);
        b.Property(i => i.Area).HasPrecision(10, 2);
        b.Property(i => i.Andar).HasMaxLength(40);
        b.Property(i => i.PrevisaoEntrega).HasMaxLength(60);
        b.Property(i => i.VideoUrl).HasMaxLength(300);
        b.Property(i => i.BairroId).HasColumnName("neighborhood_id");
        b.HasOne<Bairro>().WithMany().HasForeignKey(i => i.BairroId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(i => i.Status);
        b.HasIndex(i => i.Destaque).HasFilter("destaque");
    }
}
