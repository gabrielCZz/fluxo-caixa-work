using FluxoCaixa.Domain;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Infrastructure;

public sealed class FluxoCaixaDbContext : DbContext
{
    public FluxoCaixaDbContext(DbContextOptions<FluxoCaixaDbContext> options) : base(options)
    {
    }

    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<Subgrupo> Subgrupos => Set<Subgrupo>();
    public DbSet<RegraClassificacao> RegrasClassificacao => Set<RegraClassificacao>();
    public DbSet<Importacao> Importacoes => Set<Importacao>();
    public DbSet<SaldoInicialPeriodo> SaldosIniciais => Set<SaldoInicialPeriodo>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lancamento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContraparteNome).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(500);
            entity.Property(e => e.Valor).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Grupo).WithMany().HasForeignKey(e => e.GrupoId);
            entity.HasOne(e => e.Subgrupo).WithMany().HasForeignKey(e => e.SubgrupoId);
            entity.HasOne(e => e.Importacao).WithMany(i => i.Lancamentos).HasForeignKey(e => e.ImportacaoId);
        });

        modelBuilder.Entity<Grupo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Subgrupo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).HasMaxLength(150).IsRequired();
            entity.HasOne(e => e.Grupo).WithMany(g => g.Subgrupos).HasForeignKey(e => e.GrupoId);
        });

        modelBuilder.Entity<RegraClassificacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchContraparte).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MatchDescricaoKeywords).HasMaxLength(300);
            entity.HasOne(e => e.Grupo).WithMany().HasForeignKey(e => e.GrupoId);
            entity.HasOne(e => e.Subgrupo).WithMany().HasForeignKey(e => e.SubgrupoId);
        });

        modelBuilder.Entity<Importacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NomeArquivo).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<SaldoInicialPeriodo>(entity =>
        {
            entity.HasKey(e => e.Periodo);
            entity.Property(e => e.Periodo).HasMaxLength(7);
            entity.Property(e => e.Valor).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Usuario).WithMany().HasForeignKey(e => e.UsuarioId);
            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
        });
    }
}
