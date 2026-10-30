using GFX.SiteBruna.Dados.Data;
using GFX.SiteBruna.Modelo.Entities;
using GFX.SiteBruna.Modelo.Valores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GFX.SiteBruna.Dados.Configurations;

/// <summary>Uma linha, uma coluna jsonb por página (antes: uma linha por página com <c>conteudo_json</c>).</summary>
internal sealed class PaginasConfig : IEntityTypeConfiguration<Paginas>
{
    public void Configure(EntityTypeBuilder<Paginas> b)
    {
        b.ToTable("pages", t => t.HasCheckConstraint("ck_pages_linha_unica", $"id = {Paginas.IdUnico}"));
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.Home).HasColumnType("jsonb").HasConversion(Json.Conversor<ConteudoHome>());
        b.Property(p => p.Sobre).HasColumnType("jsonb").HasConversion(Json.Conversor<ConteudoSobre>());
        b.Property(p => p.Mcmv).HasColumnType("jsonb").HasConversion(Json.Conversor<ConteudoMcmv>());
    }
}
