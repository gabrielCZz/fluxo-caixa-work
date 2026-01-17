using FluxoCaixa.Application;
using FluxoCaixa.Domain;
using Xunit;

namespace FluxoCaixa.Tests;

public sealed class ClassificacaoEngineTests
{
    [Fact]
    public void PriorizaMatchExatoAntesDeContem()
    {
        var engine = new ClassificacaoEngine();
        var regras = new List<RegraClassificacao>
        {
            new() { Id = Guid.NewGuid(), MatchContraparte = "ABC", Modo = ModoMatch.Contem, GrupoId = Guid.NewGuid(), SubgrupoId = Guid.NewGuid(), Prioridade = 2, Ativa = true },
            new() { Id = Guid.NewGuid(), MatchContraparte = "ABC LTDA", Modo = ModoMatch.Exato, GrupoId = Guid.NewGuid(), SubgrupoId = Guid.NewGuid(), Prioridade = 1, Ativa = true }
        };

        var (grupoId, subgrupoId) = engine.Classificar(null, "ABC LTDA", "", TipoLancamento.Entrada, regras);

        Assert.Equal(regras[1].GrupoId, grupoId);
        Assert.Equal(regras[1].SubgrupoId, subgrupoId);
    }
}
