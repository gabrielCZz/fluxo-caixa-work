using FluxoCaixa.Application;
using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/importacoes")]
[Authorize(Roles = "Financeiro,Admin")]
public sealed class ImportacoesController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;
    private readonly IMongoRepository _mongoRepository;
    private readonly IDateAdjuster _dateAdjuster;
    private readonly IClassificacaoEngine _classificacaoEngine;
    private readonly ICacheService _cache;

    public ImportacoesController(FluxoCaixaDbContext db, IMongoRepository mongoRepository, IDateAdjuster dateAdjuster, IClassificacaoEngine classificacaoEngine, ICacheService cache)
    {
        _db = db;
        _mongoRepository = mongoRepository;
        _dateAdjuster = dateAdjuster;
        _classificacaoEngine = classificacaoEngine;
        _cache = cache;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<UploadResultado>> Upload([FromForm] IFormFile arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new ProblemDetails { Title = "Arquivo inválido", Status = StatusCodes.Status400BadRequest });
        }

        var importacao = new Importacao
        {
            Id = Guid.NewGuid(),
            NomeArquivo = arquivo.FileName,
            DataHora = DateTime.UtcNow,
            Usuario = User.Identity?.Name ?? "sistema",
            Status = StatusImportacao.Processando
        };

        _db.Importacoes.Add(importacao);
        await _db.SaveChangesAsync(cancellationToken);

        var parser = new ImportacaoParser();
        List<ImportacaoLinhaRaw> linhas;
        await using var stream = arquivo.OpenReadStream();

        if (Path.GetExtension(arquivo.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            linhas = parser.ParseXlsx(stream);
        }
        else
        {
            linhas = parser.ParseCsv(stream);
        }

        var lancamentos = parser.MapearLancamentos(importacao.Id, linhas, importacao.Usuario, _dateAdjuster);
        var regras = await _db.RegrasClassificacao.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var lancamento in lancamentos)
        {
            var (grupoId, subgrupoId) = _classificacaoEngine.Classificar(null, lancamento.ContraparteNome, lancamento.Descricao, lancamento.Tipo, regras);
            lancamento.GrupoId = grupoId;
            lancamento.SubgrupoId = subgrupoId;

            var duplicado = await _db.Lancamentos.AnyAsync(l => l.DataEfetiva == lancamento.DataEfetiva
                && l.Valor == lancamento.Valor
                && l.ContraparteNome == lancamento.ContraparteNome
                && l.Descricao == lancamento.Descricao
                && l.Tipo == lancamento.Tipo, cancellationToken);
            lancamento.Duplicado = duplicado;
        }

        _db.Lancamentos.AddRange(lancamentos);
        importacao.TotalLinhas = linhas.Count;
        importacao.TotalErros = linhas.Count(l => l.Erros.Count > 0);
        importacao.Status = StatusImportacao.Concluida;

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidarCacheAsync(lancamentos, cancellationToken);

        var documentos = linhas.Select(l => new ImportacaoRawDocument
        {
            ImportacaoId = importacao.Id,
            Linha = l.Linha,
            Valores = l.Valores,
            Erros = l.Erros
        });

        if (documentos.Any())
        {
            await _mongoRepository.InserirLinhasAsync(documentos, cancellationToken);
        }

        return Ok(new UploadResultado(importacao.Id, importacao.TotalLinhas, importacao.TotalErros));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ImportacaoDetalhe>> Get(Guid id, CancellationToken cancellationToken)
    {
        var importacao = await _db.Importacoes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (importacao is null)
        {
            return NotFound();
        }

        var linhas = await _mongoRepository.ObterPorImportacaoAsync(id, cancellationToken);

        return Ok(new ImportacaoDetalhe
        {
            ImportacaoId = id,
            NomeArquivo = importacao.NomeArquivo,
            TotalLinhas = importacao.TotalLinhas,
            TotalErros = importacao.TotalErros,
            Status = importacao.Status,
            Linhas = linhas.Select(l => new ImportacaoLinhaRaw
            {
                Linha = l.Linha,
                Valores = l.Valores,
                Erros = l.Erros
            }).ToList()
        });
    }

    [HttpPost("{id:guid}/aplicar-classificacao")]
    public async Task<IActionResult> AplicarClassificacao(Guid id, CancellationToken cancellationToken)
    {
        var lancamentos = await _db.Lancamentos.Where(l => l.ImportacaoId == id).ToListAsync(cancellationToken);
        if (lancamentos.Count == 0)
        {
            return NotFound();
        }

        var regras = await _db.RegrasClassificacao.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var lancamento in lancamentos)
        {
            var (grupoId, subgrupoId) = _classificacaoEngine.Classificar(null, lancamento.ContraparteNome, lancamento.Descricao, lancamento.Tipo, regras);
            lancamento.GrupoId = grupoId;
            lancamento.SubgrupoId = subgrupoId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidarCacheAsync(lancamentos, cancellationToken);
        return NoContent();
    }

    private Task InvalidarCacheAsync(IEnumerable<Lancamento> lancamentos, CancellationToken cancellationToken)
    {
        var periodos = lancamentos.Select(l => $"{l.DataEfetiva:yyyy-MM}").Distinct();
        var tasks = new List<Task>();
        foreach (var periodo in periodos)
        {
            tasks.Add(_cache.RemoverAsync($"fluxo:{periodo}:ambos", cancellationToken));
            tasks.Add(_cache.RemoverAsync($"fluxo:{periodo}:previsto", cancellationToken));
            tasks.Add(_cache.RemoverAsync($"fluxo:{periodo}:realizado", cancellationToken));
        }

        return Task.WhenAll(tasks);
    }
}
