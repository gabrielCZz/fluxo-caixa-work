using FluxoCaixa.Application;
using Xunit;

namespace FluxoCaixa.Tests;

public sealed class DateAdjusterTests
{
    [Theory]
    [InlineData("2024-05-04", "2024-05-06")]
    [InlineData("2024-05-05", "2024-05-06")]
    [InlineData("2024-05-06", "2024-05-06")]
    public void AjustaFimDeSemana(string input, string expected)
    {
        var adjuster = new DateAdjuster();
        var data = DateOnly.Parse(input);
        var resultado = adjuster.AjustarFimDeSemana(data);
        Assert.Equal(DateOnly.Parse(expected), resultado);
    }
}
