using FluxoCaixa.Domain;

namespace FluxoCaixa.Application;

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public sealed record FluxoRequest(string Periodo, string Modo);

public sealed record FluxoColuna(int Dia, DateOnly Data);

public sealed class FluxoLinha
{
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public Guid? GrupoId { get; set; }
    public Guid? SubgrupoId { get; set; }
    public Dictionary<int, decimal> ValoresPorDia { get; set; } = new();
    public decimal TotalMes { get; set; }
    public List<FluxoLinha> Filhos { get; set; } = new();
}

public sealed class FluxoResultado
{
    public List<FluxoColuna> Colunas { get; set; } = new();
    public List<FluxoLinha> Linhas { get; set; } = new();
    public decimal TotalEntradas { get; set; }
    public decimal TotalSaidas { get; set; }
    public decimal SaldoInicial { get; set; }
    public Dictionary<int, decimal> SaldosFinaisPorDia { get; set; } = new();
    public decimal SaldoFinalMes { get; set; }
}

public sealed record UploadResultado(Guid ImportacaoId, int TotalLinhas, int TotalErros);

public sealed class ImportacaoLinhaRaw
{
    public int Linha { get; set; }
    public Dictionary<string, string?> Valores { get; set; } = new();
    public List<string> Erros { get; set; } = new();
}

public sealed class ImportacaoDetalhe
{
    public Guid ImportacaoId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public int TotalLinhas { get; set; }
    public int TotalErros { get; set; }
    public StatusImportacao Status { get; set; }
    public List<ImportacaoLinhaRaw> Linhas { get; set; } = new();
}
