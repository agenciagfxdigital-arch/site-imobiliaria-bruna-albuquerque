using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

internal sealed class DadosInternosImovelConfig : IEntityTypeConfiguration<DadosInternosImovel>
{
    public void Configure(EntityTypeBuilder<DadosInternosImovel> b)
    {
        b.ToTable("property_internal");
        b.HasKey(d => d.ImovelId);
        b.Property(d => d.ImovelId).HasColumnName("property_id").ValueGeneratedNever();
        b.Property(d => d.ProprietarioNome).HasMaxLength(DadosInternosImovel.TamanhoMaximoProprietario);
        b.Property(d => d.Endereco).HasMaxLength(DadosInternosImovel.TamanhoMaximoEndereco);
        b.Property(d => d.AtualizadoPor).HasMaxLength(80);
        b.HasOne<Imovel>().WithOne().HasForeignKey<DadosInternosImovel>(d => d.ImovelId).OnDelete(DeleteBehavior.Cascade);
    }
}
