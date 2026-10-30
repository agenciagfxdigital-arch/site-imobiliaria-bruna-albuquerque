using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

/// <summary>
/// Sem índice único de capa por imóvel: trocar a capa atualiza duas linhas no mesmo SaveChanges e o Postgres
/// confere índice único linha a linha. Uma capa só é regra do Modelo (<c>PlanejamentoDeFotos</c>).
/// </summary>
internal sealed class FotoImovelConfig : IEntityTypeConfiguration<FotoImovel>
{
    public void Configure(EntityTypeBuilder<FotoImovel> b)
    {
        b.ToTable("property_photos");
        b.HasKey(f => f.Id);
        b.Property(f => f.Id).ValueGeneratedNever();
        b.Property(f => f.ImovelId).HasColumnName("property_id");
        b.Property(f => f.Url).HasMaxLength(1000);
        b.Property(f => f.CaminhoNoStorage).HasColumnName("storage_path").HasMaxLength(500);
        b.HasOne<Imovel>().WithMany().HasForeignKey(f => f.ImovelId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(f => new { f.ImovelId, f.Ordem });
        b.HasIndex(f => f.CaminhoNoStorage);
    }
}
