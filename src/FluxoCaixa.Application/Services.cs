using System.Globalization;
using FluxoCaixa.Domain;

namespace FluxoCaixa.Application;

public interface IDateAdjuster
{
    DateOnly AjustarFimDeSemana(DateOnly data);
}

public sealed class DateAdjuster : IDateAdjuster
{
    public DateOnly AjustarFimDeSemana(DateOnly data)
    {
        var dayOfWeek = data.DayOfWeek;
        if (dayOfWeek == DayOfWeek.Saturday)
        {
            return data.AddDays(2);
        }

        if (dayOfWeek == DayOfWeek.Sunday)
        {
            return data.AddDays(1);
        }

        return data;
    }
}

public interface IClassificacaoEngine
{
    (Guid? GrupoId, Guid? SubgrupoId) Classificar(RegraClassificacao? regra, string contraparte, string? descricao, TipoLancamento tipo, IEnumerable<RegraClassificacao> regras);
}

public sealed class ClassificacaoEngine : IClassificacaoEngine
{
    public (Guid? GrupoId, Guid? SubgrupoId) Classificar(RegraClassificacao? regra, string contraparte, string? descricao, TipoLancamento tipo, IEnumerable<RegraClassificacao> regras)
    {
        if (regra is not null)
        {
            return (regra.GrupoId, regra.SubgrupoId);
        }

        var regrasAtivas = regras.Where(r => r.Ativa).OrderBy(r => r.Prioridade).ToList();
        var contraparteNormalizada = Normalizar(contraparte);
        var descricaoNormalizada = Normalizar(descricao ?? string.Empty);

        var matchExato = regrasAtivas
            .Where(r => r.Modo == ModoMatch.Exato)
            .FirstOrDefault(r => (r.MatchTipoLancamento is null || r.MatchTipoLancamento == tipo) && r.LetMatch(contraparteNormalizada, descricaoNormalizada) is not null);

        if (matchExato is not null)
        {
            return (matchExato.GrupoId, matchExato.SubgrupoId);
        }

        var matchContem = regrasAtivas
            .Where(r => r.Modo == ModoMatch.Contem)
            .FirstOrDefault(r => (r.MatchTipoLancamento is null || r.MatchTipoLancamento == tipo) && r.LetMatch(contraparteNormalizada, descricaoNormalizada) is not null);

        if (matchContem is not null)
        {
            return (matchContem.GrupoId, matchContem.SubgrupoId);
        }

        var matchDescricao = regrasAtivas
            .Where(r => !string.IsNullOrWhiteSpace(r.MatchDescricaoKeywords))
            .FirstOrDefault(r => (r.MatchTipoLancamento is null || r.MatchTipoLancamento == tipo) && r.LetDescricaoMatch(descricaoNormalizada) is not null);

        if (matchDescricao is not null)
        {
            return (matchDescricao.GrupoId, matchDescricao.SubgrupoId);
        }

        return (null, null);
    }

    private static string Normalizar(string texto)
    {
        return texto.Trim().ToLowerInvariant();
    }
}

internal static class RegraClassificacaoExtensions
{
    public static RegraClassificacao? LetMatch(this RegraClassificacao regra, string contraparteNormalizada, string descricaoNormalizada)
    {
        var alvo = regra.MatchContraparte.Trim().ToLowerInvariant();
        var match = regra.Modo == ModoMatch.Exato
            ? contraparteNormalizada == alvo
            : contraparteNormalizada.Contains(alvo, StringComparison.OrdinalIgnoreCase);

        if (!match)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(regra.MatchDescricaoKeywords))
        {
            var palavras = regra.MatchDescricaoKeywords.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (palavras.Length > 0 && !palavras.Any(p => descricaoNormalizada.Contains(p.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }
        }

        return regra;
    }

    public static RegraClassificacao? LetDescricaoMatch(this RegraClassificacao regra, string descricaoNormalizada)
    {
        if (string.IsNullOrWhiteSpace(regra.MatchDescricaoKeywords))
        {
            return null;
        }

        var palavras = regra.MatchDescricaoKeywords.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (palavras.Any(p => descricaoNormalizada.Contains(p.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)))
        {
            return regra;
        }

        return null;
    }
}

public interface IFluxoCalculator
{
    FluxoResultado Calcular(string periodo, decimal saldoInicial, IEnumerable<Lancamento> lancamentos, IEnumerable<Grupo> grupos, IEnumerable<Subgrupo> subgrupos);
}

public sealed class FluxoCalculator : IFluxoCalculator
{
    public FluxoResultado Calcular(string periodo, decimal saldoInicial, IEnumerable<Lancamento> lancamentos, IEnumerable<Grupo> grupos, IEnumerable<Subgrupo> subgrupos)
    {
        var dataInicio = DateOnly.ParseExact($"{periodo}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var diasNoMes = DateTime.DaysInMonth(dataInicio.Year, dataInicio.Month);
        var colunas = Enumerable.Range(1, diasNoMes)
            .Select(dia => new FluxoColuna(dia, new DateOnly(dataInicio.Year, dataInicio.Month, dia)))
            .ToList();

        var lancamentosMes = lancamentos.Where(l => l.DataEfetiva.Year == dataInicio.Year && l.DataEfetiva.Month == dataInicio.Month).ToList();
        var linhas = new List<FluxoLinha>();

        foreach (var grupo in grupos.OrderBy(g => g.Ordem))
        {
            var linhaGrupo = new FluxoLinha
            {
                Nome = grupo.Nome,
                Tipo = "Grupo",
                GrupoId = grupo.Id
            };

            var subgruposDoGrupo = subgrupos.Where(s => s.GrupoId == grupo.Id).OrderBy(s => s.Ordem).ToList();
            foreach (var subgrupo in subgruposDoGrupo)
            {
                var linhaSubgrupo = new FluxoLinha
                {
                    Nome = subgrupo.Nome,
                    Tipo = "Subgrupo",
                    GrupoId = grupo.Id,
                    SubgrupoId = subgrupo.Id
                };

                var lancamentosSubgrupo = lancamentosMes.Where(l => l.SubgrupoId == subgrupo.Id).ToList();
                foreach (var lancamento in lancamentosSubgrupo)
                {
                    var dia = lancamento.DataEfetiva.Day;
                    linhaSubgrupo.ValoresPorDia[dia] = linhaSubgrupo.ValoresPorDia.GetValueOrDefault(dia) + lancamento.Valor;
                    linhaSubgrupo.TotalMes += lancamento.Valor;
                }

                linhaGrupo.Filhos.Add(linhaSubgrupo);
                foreach (var valorDia in linhaSubgrupo.ValoresPorDia)
                {
                    linhaGrupo.ValoresPorDia[valorDia.Key] = linhaGrupo.ValoresPorDia.GetValueOrDefault(valorDia.Key) + valorDia.Value;
                }

                linhaGrupo.TotalMes += linhaSubgrupo.TotalMes;
            }

            linhas.Add(linhaGrupo);
        }

        var totalEntradas = lancamentosMes.Where(l => l.Tipo == TipoLancamento.Entrada).Sum(l => l.Valor);
        var totalSaidas = lancamentosMes.Where(l => l.Tipo == TipoLancamento.Saida).Sum(l => l.Valor);
        var saldosFinaisPorDia = new Dictionary<int, decimal>();
        var saldoAtual = saldoInicial;

        foreach (var coluna in colunas)
        {
            var entradasDia = lancamentosMes.Where(l => l.Tipo == TipoLancamento.Entrada && l.DataEfetiva.Day == coluna.Dia).Sum(l => l.Valor);
            var saidasDia = lancamentosMes.Where(l => l.Tipo == TipoLancamento.Saida && l.DataEfetiva.Day == coluna.Dia).Sum(l => l.Valor);
            saldoAtual += entradasDia - saidasDia;
            saldosFinaisPorDia[coluna.Dia] = saldoAtual;
        }

        return new FluxoResultado
        {
            Colunas = colunas,
            Linhas = linhas,
            TotalEntradas = totalEntradas,
            TotalSaidas = totalSaidas,
            SaldoInicial = saldoInicial,
            SaldosFinaisPorDia = saldosFinaisPorDia,
            SaldoFinalMes = saldoAtual
        };
    }
}
