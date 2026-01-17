using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api;

public sealed class SeedDataHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public SeedDataHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FluxoCaixaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Grupos.AnyAsync(cancellationToken))
        {
            var grupoEntrada = new Grupo
            {
                Id = Guid.NewGuid(),
                Nome = "Receitas",
                Tipo = TipoLancamento.Entrada,
                Ordem = 1,
                Ativo = true
            };
            var grupoSaida = new Grupo
            {
                Id = Guid.NewGuid(),
                Nome = "Despesas",
                Tipo = TipoLancamento.Saida,
                Ordem = 2,
                Ativo = true
            };
            var grupoNaoClassificadoEntrada = new Grupo
            {
                Id = Guid.NewGuid(),
                Nome = "Não classificado (Entrada)",
                Tipo = TipoLancamento.Entrada,
                Ordem = 99,
                Ativo = true
            };
            var grupoNaoClassificadoSaida = new Grupo
            {
                Id = Guid.NewGuid(),
                Nome = "Não classificado (Saída)",
                Tipo = TipoLancamento.Saida,
                Ordem = 99,
                Ativo = true
            };

            db.Grupos.AddRange(grupoEntrada, grupoSaida, grupoNaoClassificadoEntrada, grupoNaoClassificadoSaida);
            db.Subgrupos.AddRange(
                new Subgrupo { Id = Guid.NewGuid(), GrupoId = grupoEntrada.Id, Nome = "Vendas", Ordem = 1, Ativo = true },
                new Subgrupo { Id = Guid.NewGuid(), GrupoId = grupoSaida.Id, Nome = "Operacionais", Ordem = 1, Ativo = true },
                new Subgrupo { Id = Guid.NewGuid(), GrupoId = grupoNaoClassificadoEntrada.Id, Nome = "Não classificado", Ordem = 1, Ativo = true },
                new Subgrupo { Id = Guid.NewGuid(), GrupoId = grupoNaoClassificadoSaida.Id, Nome = "Não classificado", Ordem = 1, Ativo = true }
            );
        }

        if (!await db.Usuarios.AnyAsync(cancellationToken))
        {
            var admin = new Usuario
            {
                Id = Guid.NewGuid(),
                Email = "admin@fluxocaixa.local",
                PasswordHash = hasher.Hash("Admin@123"),
                Roles = "Admin,Financeiro"
            };

            db.Usuarios.Add(admin);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
