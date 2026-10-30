using GFX.SiteBruna.Modelo.Entities;
using Microsoft.EntityFrameworkCore;

namespace GFX.SiteBruna.Dados.Data;

/// <summary>
/// Schema do site, que muda só por migrations (aplicadas pelo dev). Fica no schema <c>site</c>, e não no <c>public</c>:
/// o Supabase expõe o <c>public</c> pela Data API para a chave anônima, e aqui ninguém lê o banco por fora da Api.
/// </summary>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string Schema = "site";
    public const string SequenciaDeCodigo = "property_code_seq";

    public DbSet<Imovel> Imovel => Set<Imovel>();
    public DbSet<FotoImovel> FotoImovel => Set<FotoImovel>();
    public DbSet<DadosInternosImovel> DadosInternosImovel => Set<DadosInternosImovel>();
    public DbSet<Bairro> Bairro => Set<Bairro>();
    public DbSet<Operador> Operador => Set<Operador>();
    public DbSet<AcaoPendente> AcaoPendente => Set<AcaoPendente>();
    public DbSet<RegistroDeAuditoria> RegistroDeAuditoria => Set<RegistroDeAuditoria>();
    public DbSet<MensagemDoWhatsapp> MensagemDoWhatsapp => Set<MensagemDoWhatsapp>();
    public DbSet<Configuracoes> Configuracoes => Set<Configuracoes>();
    public DbSet<Paginas> Paginas => Set<Paginas>();
    public DbSet<Faq> Faq => Set<Faq>();
    public DbSet<Depoimento> Depoimento => Set<Depoimento>();
    public DbSet<Evento> Evento => Set<Evento>();
    public DbSet<Usuario> Usuario => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasSequence<long>(SequenciaDeCodigo).StartsAt(101);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
