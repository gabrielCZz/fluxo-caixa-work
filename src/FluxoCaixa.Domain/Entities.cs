namespace FluxoCaixa.Domain;

public sealed class Lancamento
{
    public Guid Id { get; set; }
    public TipoLancamento Tipo { get; set; }
    public DateOnly DataVencimentoOriginal { get; set; }
    public DateOnly DataEfetiva { get; set; }
    public string ContraparteNome { get; set; } = string.Empty;
    public string? ContraparteDocumento { get; set; }
    public string? Descricao { get; set; }
    public decimal Valor { get; set; }
    public Guid? GrupoId { get; set; }
    public Guid? SubgrupoId { get; set; }
    public StatusLancamento Status { get; set; }
    public OrigemLancamento Origem { get; set; }
    public Guid? ImportacaoId { get; set; }
    public bool Duplicado { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public Grupo? Grupo { get; set; }
    public Subgrupo? Subgrupo { get; set; }
    public Importacao? Importacao { get; set; }
}

public sealed class Grupo
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoLancamento Tipo { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; }

    public ICollection<Subgrupo> Subgrupos { get; set; } = new List<Subgrupo>();
}

public sealed class Subgrupo
{
    public Guid Id { get; set; }
    public Guid GrupoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Ativo { get; set; }

    public Grupo? Grupo { get; set; }
}

public sealed class RegraClassificacao
{
    public Guid Id { get; set; }
    public string MatchContraparte { get; set; } = string.Empty;
    public ModoMatch Modo { get; set; }
    public string? MatchDescricaoKeywords { get; set; }
    public TipoLancamento? MatchTipoLancamento { get; set; }
    public Guid GrupoId { get; set; }
    public Guid SubgrupoId { get; set; }
    public int Prioridade { get; set; }
    public bool Ativa { get; set; }

    public Grupo? Grupo { get; set; }
    public Subgrupo? Subgrupo { get; set; }
}

public sealed class Importacao
{
    public Guid Id { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int TotalLinhas { get; set; }
    public int TotalErros { get; set; }
    public StatusImportacao Status { get; set; }

    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}

public sealed class SaldoInicialPeriodo
{
    public string Periodo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string DefinidoPor { get; set; } = string.Empty;
    public DateTime DefinidoEm { get; set; }
}

public sealed class Usuario
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Revogado { get; set; }

    public Usuario? Usuario { get; set; }
}
