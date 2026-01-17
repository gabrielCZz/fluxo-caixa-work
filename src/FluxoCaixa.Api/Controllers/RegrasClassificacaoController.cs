using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/regras-classificacao")]
[Authorize(Roles = "Admin")]
public sealed class RegrasClassificacaoController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;

    public RegrasClassificacaoController(FluxoCaixaDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<RegraClassificacao>>> Get(CancellationToken cancellationToken)
    {
        var regras = await _db.RegrasClassificacao.AsNoTracking().OrderBy(r => r.Prioridade).ToListAsync(cancellationToken);
        return Ok(regras);
    }

    [HttpPost]
    public async Task<ActionResult<RegraClassificacao>> Post([FromBody] RegraClassificacaoRequest request, CancellationToken cancellationToken)
    {
        var regra = new RegraClassificacao
        {
            Id = Guid.NewGuid(),
            MatchContraparte = request.MatchContraparte,
            Modo = request.Modo,
            MatchDescricaoKeywords = request.MatchDescricaoKeywords,
            MatchTipoLancamento = request.MatchTipoLancamento,
            GrupoId = request.GrupoId,
            SubgrupoId = request.SubgrupoId,
            Prioridade = request.Prioridade,
            Ativa = request.Ativa
        };

        _db.RegrasClassificacao.Add(regra);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = regra.Id }, regra);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RegraClassificacao>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var regra = await _db.RegrasClassificacao.FindAsync(new object?[] { id }, cancellationToken);
        if (regra is null)
        {
            return NotFound();
        }

        return Ok(regra);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] RegraClassificacaoRequest request, CancellationToken cancellationToken)
    {
        var regra = await _db.RegrasClassificacao.FindAsync(new object?[] { id }, cancellationToken);
        if (regra is null)
        {
            return NotFound();
        }

        regra.MatchContraparte = request.MatchContraparte;
        regra.Modo = request.Modo;
        regra.MatchDescricaoKeywords = request.MatchDescricaoKeywords;
        regra.MatchTipoLancamento = request.MatchTipoLancamento;
        regra.GrupoId = request.GrupoId;
        regra.SubgrupoId = request.SubgrupoId;
        regra.Prioridade = request.Prioridade;
        regra.Ativa = request.Ativa;

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var regra = await _db.RegrasClassificacao.FindAsync(new object?[] { id }, cancellationToken);
        if (regra is null)
        {
            return NotFound();
        }

        _db.RegrasClassificacao.Remove(regra);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
