using FluxoCaixa.Application;
using FluxoCaixa.Domain;
using Xunit;

namespace FluxoCaixa.Tests;

public sealed class FluxoCalculatorTests
{
    [Fact]
    public void CalculaSaldoFinalPorDia()
    {
        var calculator = new FluxoCalculator();
        var lancamentos = new List<Lancamento>
        {
            new() { DataEfetiva = new DateOnly(2024, 5, 1), Tipo = TipoLancamento.Entrada, Valor = 100 },
            new() { DataEfetiva = new DateOnly(2024, 5, 1), Tipo = TipoLancamento.Saida, Valor = 40 },
            new() { DataEfetiva = new DateOnly(2024, 5, 2), Tipo = TipoLancamento.Saida, Valor = 10 }
        };
        var grupos = new List<Grupo>();
        var subgrupos = new List<Subgrupo>();

        var resultado = calculator.Calcular("2024-05", 50, lancamentos, grupos, subgrupos);

        Assert.Equal(110, resultado.SaldosFinaisPorDia[1]);
        Assert.Equal(100, resultado.SaldosFinaisPorDia[2]);
        Assert.Equal(100, resultado.SaldoFinalMes);
    }
}
