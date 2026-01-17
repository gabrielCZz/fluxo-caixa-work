using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/grupos")]
[Authorize(Roles = "Admin")]
public sealed class GruposController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;

    public GruposController(FluxoCaixaDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<Grupo>>> Get(CancellationToken cancellationToken)
    {
        var grupos = await _db.Grupos.AsNoTracking().OrderBy(g => g.Ordem).ToListAsync(cancellationToken);
        return Ok(grupos);
    }

    [HttpPost]
    public async Task<ActionResult<Grupo>> Post([FromBody] GrupoRequest request, CancellationToken cancellationToken)
    {
        var grupo = new Grupo
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Tipo = request.Tipo,
            Ordem = request.Ordem,
            Ativo = request.Ativo
        };

        _db.Grupos.Add(grupo);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = grupo.Id }, grupo);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Grupo>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var grupo = await _db.Grupos.FindAsync(new object?[] { id }, cancellationToken);
        if (grupo is null)
        {
            return NotFound();
        }

        return Ok(grupo);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] GrupoRequest request, CancellationToken cancellationToken)
    {
        var grupo = await _db.Grupos.FindAsync(new object?[] { id }, cancellationToken);
        if (grupo is null)
        {
            return NotFound();
        }

        grupo.Nome = request.Nome;
        grupo.Tipo = request.Tipo;
        grupo.Ordem = request.Ordem;
        grupo.Ativo = request.Ativo;

        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var grupo = await _db.Grupos.FindAsync(new object?[] { id }, cancellationToken);
        if (grupo is null)
        {
            return NotFound();
        }

        _db.Grupos.Remove(grupo);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
