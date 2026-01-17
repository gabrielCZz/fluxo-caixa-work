using FluxoCaixa.Application;
using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/lancamentos")]
[Authorize]
public sealed class LancamentosController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;
    private readonly IDateAdjuster _dateAdjuster;
    private readonly ICacheService _cache;

    public LancamentosController(FluxoCaixaDbContext db, IDateAdjuster dateAdjuster, ICacheService cache)
    {
        _db = db;
        _dateAdjuster = dateAdjuster;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<List<Lancamento>>> Get([FromQuery] string? periodo, [FromQuery] TipoLancamento? tipo, [FromQuery] Guid? grupoId, [FromQuery] Guid? subgrupoId, [FromQuery] string? contraparte, [FromQuery] bool? naoClassificados, CancellationToken cancellationToken)
    {
        var query = _db.Lancamentos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            var data = DateOnly.ParseExact($"{periodo}-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            query = query.Where(l => l.DataEfetiva.Year == data.Year && l.DataEfetiva.Month == data.Month);
        }

        if (tipo.HasValue)
        {
            query = query.Where(l => l.Tipo == tipo.Value);
        }

        if (grupoId.HasValue)
        {
            query = query.Where(l => l.GrupoId == grupoId.Value);
        }

        if (subgrupoId.HasValue)
        {
            query = query.Where(l => l.SubgrupoId == subgrupoId.Value);
        }

        if (naoClassificados == true)
        {
            query = query.Where(l => l.GrupoId == null || l.SubgrupoId == null);
        }

        if (!string.IsNullOrWhiteSpace(contraparte))
        {
            query = query.Where(l => l.ContraparteNome.Contains(contraparte));
        }

        var items = await query.OrderBy(l => l.DataEfetiva).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Financeiro,Admin")]
    public async Task<ActionResult<Lancamento>> Post([FromBody] LancamentoRequest request, CancellationToken cancellationToken)
    {
        var dataEfetiva = _dateAdjuster.AjustarFimDeSemana(request.DataVencimentoOriginal);
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            Tipo = request.Tipo,
            DataVencimentoOriginal = request.DataVencimentoOriginal,
            DataEfetiva = dataEfetiva,
            ContraparteNome = request.ContraparteNome.Trim(),
            ContraparteDocumento = request.ContraparteDocumento,
            Descricao = request.Descricao?.Trim(),
            Valor = request.Valor,
            GrupoId = request.GrupoId,
            SubgrupoId = request.SubgrupoId,
            Status = request.Status,
            Origem = request.Origem,
            ImportacaoId = request.ImportacaoId,
            Duplicado = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "sistema",
            UpdatedBy = User.Identity?.Name ?? "sistema"
        };

        _db.Lancamentos.Add(lancamento);
        await _db.SaveChangesAsync(cancellationToken);
        await InvalidarCacheAsync(lancamento.DataEfetiva, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = lancamento.Id }, lancamento);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Lancamento>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lancamento = await _db.Lancamentos.FindAsync(new object?[] { id }, cancellationToken);
        if (lancamento is null)
        {
            return NotFound();
        }

        return Ok(lancamento);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Financeiro,Admin")]
    public async Task<IActionResult> Put(Guid id, [FromBody] LancamentoRequest request, CancellationToken cancellationToken)
    {
        var lancamento = await _db.Lancamentos.FindAsync(new object?[] { id }, cancellationToken);
        if (lancamento is null)
        {
            return NotFound();
        }

        lancamento.Tipo = request.Tipo;
        lancamento.DataVencimentoOriginal = request.DataVencimentoOriginal;
        lancamento.DataEfetiva = _dateAdjuster.AjustarFimDeSemana(request.DataVencimentoOriginal);
        lancamento.ContraparteNome = request.ContraparteNome.Trim();
        lancamento.ContraparteDocumento = request.ContraparteDocumento;
        lancamento.Descricao = request.Descricao?.Trim();
        lancamento.Valor = request.Valor;
        lancamento.GrupoId = request.GrupoId;
        lancamento.SubgrupoId = request.SubgrupoId;
        lancamento.Status = request.Status;
        lancamento.Origem = request.Origem;
        lancamento.ImportacaoId = request.ImportacaoId;
        lancamento.UpdatedAt = DateTime.UtcNow;
        lancamento.UpdatedBy = User.Identity?.Name ?? "sistema";

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidarCacheAsync(lancamento.DataEfetiva, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Financeiro,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var lancamento = await _db.Lancamentos.FindAsync(new object?[] { id }, cancellationToken);
        if (lancamento is null)
        {
            return NotFound();
        }

        _db.Lancamentos.Remove(lancamento);
        await _db.SaveChangesAsync(cancellationToken);
        await InvalidarCacheAsync(lancamento.DataEfetiva, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/classificar")]
    [Authorize(Roles = "Financeiro,Admin")]
    public async Task<IActionResult> Classificar(Guid id, [FromBody] LancamentoClassificacaoRequest request, CancellationToken cancellationToken)
    {
        var lancamento = await _db.Lancamentos.FindAsync(new object?[] { id }, cancellationToken);
        if (lancamento is null)
        {
            return NotFound();
        }

        lancamento.GrupoId = request.GrupoId;
        lancamento.SubgrupoId = request.SubgrupoId;
        lancamento.UpdatedAt = DateTime.UtcNow;
        lancamento.UpdatedBy = User.Identity?.Name ?? "sistema";

        await _db.SaveChangesAsync(cancellationToken);
        await InvalidarCacheAsync(lancamento.DataEfetiva, cancellationToken);
        return NoContent();
    }

    private Task InvalidarCacheAsync(DateOnly dataEfetiva, CancellationToken cancellationToken)
    {
        var periodo = $"{dataEfetiva:yyyy-MM}";
        return Task.WhenAll(
            _cache.RemoverAsync($"fluxo:{periodo}:ambos", cancellationToken),
            _cache.RemoverAsync($"fluxo:{periodo}:previsto", cancellationToken),
            _cache.RemoverAsync($"fluxo:{periodo}:realizado", cancellationToken)
        );
    }
}
