using ClosedXML.Excel;
using FluxoCaixa.Application;
using FluxoCaixa.Domain;
using FluxoCaixa.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Api.Controllers;

[ApiController]
[Route("api/fluxo")]
[Authorize]
public sealed class FluxoController : ControllerBase
{
    private readonly FluxoCaixaDbContext _db;
    private readonly IFluxoCalculator _calculator;
    private readonly ICacheService _cache;

    public FluxoController(FluxoCaixaDbContext db, IFluxoCalculator calculator, ICacheService cache)
    {
        _db = db;
        _calculator = calculator;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<FluxoResultado>> Get([FromQuery] string periodo, [FromQuery] string modo = "ambos", CancellationToken cancellationToken = default)
    {
        var cacheKey = $"fluxo:{periodo}:{modo}";
        var cached = await _cache.GetAsync<FluxoResultado>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var lancamentosQuery = _db.Lancamentos.AsNoTracking().AsQueryable();
        if (modo.Equals("previsto", StringComparison.OrdinalIgnoreCase))
        {
            lancamentosQuery = lancamentosQuery.Where(l => l.Status == StatusLancamento.Previsto);
        }
        else if (modo.Equals("realizado", StringComparison.OrdinalIgnoreCase))
        {
            lancamentosQuery = lancamentosQuery.Where(l => l.Status == StatusLancamento.Realizado);
        }

        var lancamentos = await lancamentosQuery.ToListAsync(cancellationToken);
        var grupos = await _db.Grupos.AsNoTracking().Where(g => g.Ativo).ToListAsync(cancellationToken);
        var subgrupos = await _db.Subgrupos.AsNoTracking().Where(s => s.Ativo).ToListAsync(cancellationToken);
        var saldoInicial = await _db.SaldosIniciais.AsNoTracking().Where(s => s.Periodo == periodo).Select(s => s.Valor).FirstOrDefaultAsync(cancellationToken);

        var resultado = _calculator.Calcular(periodo, saldoInicial, lancamentos, grupos, subgrupos);
        await _cache.SetAsync(cacheKey, resultado, TimeSpan.FromMinutes(10), cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string periodo, [FromQuery] string modo = "ambos", CancellationToken cancellationToken = default)
    {
        var resultado = await Get(periodo, modo, cancellationToken);
        if (resultado.Result is not null)
        {
            return resultado.Result;
        }

        var dados = resultado.Value!;
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Fluxo");
        worksheet.Cell(1, 1).Value = "Grupo/Subgrupo";

        for (var i = 0; i < dados.Colunas.Count; i++)
        {
            worksheet.Cell(1, i + 2).Value = dados.Colunas[i].Dia.ToString("00");
        }

        worksheet.Cell(1, dados.Colunas.Count + 2).Value = "Total";
        var linhaAtual = 2;

        foreach (var linha in dados.Linhas)
        {
            linhaAtual = EscreverLinha(worksheet, linha, dados.Colunas, linhaAtual, 0);
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"fluxo-{periodo}.xlsx");
    }

    private static int EscreverLinha(IXLWorksheet worksheet, FluxoLinha linha, List<FluxoColuna> colunas, int linhaAtual, int nivel)
    {
        worksheet.Cell(linhaAtual, 1).Value = new string(' ', nivel * 2) + linha.Nome;
        for (var i = 0; i < colunas.Count; i++)
        {
            var dia = colunas[i].Dia;
            worksheet.Cell(linhaAtual, i + 2).Value = linha.ValoresPorDia.GetValueOrDefault(dia);
        }

        worksheet.Cell(linhaAtual, colunas.Count + 2).Value = linha.TotalMes;
        linhaAtual++;

        foreach (var filho in linha.Filhos)
        {
            linhaAtual = EscreverLinha(worksheet, filho, colunas, linhaAtual, nivel + 1);
        }

        return linhaAtual;
    }
}
