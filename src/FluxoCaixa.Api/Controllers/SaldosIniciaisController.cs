using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/saldos-iniciais")]
[Authorize(Roles = "Financeiro,Admin")]
public sealed class SaldosIniciaisController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;
    private readonly ICacheService _cache;

    public SaldosIniciaisController(FluxoCaixaDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet("{periodo}")]
    public async Task<ActionResult<SaldoInicialPeriodo>> Get(string periodo, CancellationToken cancellationToken)
    {
        var saldo = await _db.SaldosIniciais.AsNoTracking().FirstOrDefaultAsync(s => s.Periodo == periodo, cancellationToken);
        if (saldo is null)
        {
            return NotFound();
        }

        return Ok(saldo);
    }

    [HttpPut("{periodo}")]
    public async Task<IActionResult> Put(string periodo, [FromBody] SaldoInicialRequest request, CancellationToken cancellationToken)
    {
        var saldo = await _db.SaldosIniciais.FirstOrDefaultAsync(s => s.Periodo == periodo, cancellationToken);
        if (saldo is null)
        {
            saldo = new SaldoInicialPeriodo
            {
                Periodo = periodo,
                Valor = request.Valor,
                DefinidoPor = request.DefinidoPor,
                DefinidoEm = DateTime.UtcNow
            };
            _db.SaldosIniciais.Add(saldo);
        }
        else
        {
            saldo.Valor = request.Valor;
            saldo.DefinidoPor = request.DefinidoPor;
            saldo.DefinidoEm = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoverAsync($"fluxo:{periodo}:ambos", cancellationToken);
        await _cache.RemoverAsync($"fluxo:{periodo}:previsto", cancellationToken);
        await _cache.RemoverAsync($"fluxo:{periodo}:realizado", cancellationToken);
        return NoContent();
    }
}
