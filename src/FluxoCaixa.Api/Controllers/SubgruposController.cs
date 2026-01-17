using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/subgrupos")]
[Authorize(Roles = "Admin")]
public sealed class SubgruposController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;

    public SubgruposController(FluxoCaixaDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<Subgrupo>>> Get(CancellationToken cancellationToken)
    {
        var subgrupos = await _db.Subgrupos.AsNoTracking().OrderBy(s => s.Ordem).ToListAsync(cancellationToken);
        return Ok(subgrupos);
    }

    [HttpPost]
    public async Task<ActionResult<Subgrupo>> Post([FromBody] SubgrupoRequest request, CancellationToken cancellationToken)
    {
        var subgrupo = new Subgrupo
        {
            Id = Guid.NewGuid(),
            GrupoId = request.GrupoId,
            Nome = request.Nome,
            Ordem = request.Ordem,
            Ativo = request.Ativo
        };

        _db.Subgrupos.Add(subgrupo);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = subgrupo.Id }, subgrupo);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Subgrupo>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var subgrupo = await _db.Subgrupos.FindAsync(new object?[] { id }, cancellationToken);
        if (subgrupo is null)
        {
            return NotFound();
        }

        return Ok(subgrupo);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] SubgrupoRequest request, CancellationToken cancellationToken)
    {
        var subgrupo = await _db.Subgrupos.FindAsync(new object?[] { id }, cancellationToken);
        if (subgrupo is null)
        {
            return NotFound();
        }

        subgrupo.Nome = request.Nome;
        subgrupo.GrupoId = request.GrupoId;
        subgrupo.Ordem = request.Ordem;
        subgrupo.Ativo = request.Ativo;

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var subgrupo = await _db.Subgrupos.FindAsync(new object?[] { id }, cancellationToken);
        if (subgrupo is null)
        {
            return NotFound();
        }

        _db.Subgrupos.Remove(subgrupo);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
