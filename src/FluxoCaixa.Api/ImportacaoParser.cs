using ClosedXML.Excel;
using FluxoCaixa.Application;
using FluxoCaixa.Domain;

namespace FluxoCaixa.Api;

public sealed class ImportacaoParser
{
    public List<ImportacaoLinhaRaw> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var linhas = new List<ImportacaoLinhaRaw>();
        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            return linhas;
        }

        var separator = headerLine.Contains(';') ? ';' : ',';
        var headers = headerLine.Split(separator).Select(h => h.Trim()).ToArray();
        var linhaIndex = 1;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            linhaIndex++;
            var values = line.Split(separator);
            var dict = new Dictionary<string, string?>();
            for (var i = 0; i < headers.Length; i++)
            {
                dict[headers[i]] = i < values.Length ? values[i].Trim() : null;
            }

            linhas.Add(new ImportacaoLinhaRaw { Linha = linhaIndex, Valores = dict });
        }

        return linhas;
    }

    public List<ImportacaoLinhaRaw> ParseXlsx(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var rows = worksheet.RowsUsed().ToList();
        var linhas = new List<ImportacaoLinhaRaw>();
        if (rows.Count == 0)
        {
            return linhas;
        }

        var headers = rows[0].Cells().Select(c => c.GetString().Trim()).ToArray();

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var dict = new Dictionary<string, string?>();
            for (var col = 0; col < headers.Length; col++)
            {
                dict[headers[col]] = row.Cell(col + 1).GetString().Trim();
            }

            linhas.Add(new ImportacaoLinhaRaw { Linha = i + 1, Valores = dict });
        }

        return linhas;
    }

    public List<Lancamento> MapearLancamentos(Guid importacaoId, IEnumerable<ImportacaoLinhaRaw> linhas, string usuario, IDateAdjuster dateAdjuster)
    {
        var lancamentos = new List<Lancamento>();
        foreach (var linha in linhas)
        {
            var erros = new List<string>();
            if (!TryGetDate(linha, "DataVencimento", out var dataVencimento))
            {
                erros.Add("DataVencimento inválida");
            }

            var tipoStr = GetValor(linha, "Tipo");
            var tipo = ParseTipo(tipoStr);
            if (tipo is null)
            {
                erros.Add("Tipo inválido");
            }

            var valorStr = GetValor(linha, "Valor");
            if (!decimal.TryParse(valorStr, out var valor))
            {
                erros.Add("Valor inválido");
            }

            var contraparte = GetValor(linha, "Contraparte");
            if (string.IsNullOrWhiteSpace(contraparte))
            {
                erros.Add("Contraparte obrigatória");
            }

            var statusStr = GetValor(linha, "Status");
            var status = ParseStatus(statusStr) ?? StatusLancamento.Previsto;

            if (erros.Count > 0)
            {
                linha.Erros.AddRange(erros);
                continue;
            }

            var dataEfetiva = dateAdjuster.AjustarFimDeSemana(dataVencimento);
            lancamentos.Add(new Lancamento
            {
                Id = Guid.NewGuid(),
                Tipo = tipo!.Value,
                DataVencimentoOriginal = dataVencimento,
                DataEfetiva = dataEfetiva,
                ContraparteNome = contraparte.Trim(),
                ContraparteDocumento = GetValor(linha, "Documento"),
                Descricao = GetValor(linha, "Descricao"),
                Valor = valor,
                Status = status,
                Origem = OrigemLancamento.Importado,
                ImportacaoId = importacaoId,
                Duplicado = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = usuario,
                UpdatedBy = usuario
            });
        }

        return lancamentos;
    }

    private static bool TryGetDate(ImportacaoLinhaRaw linha, string coluna, out DateOnly data)
    {
        var valor = GetValor(linha, coluna);
        if (DateOnly.TryParse(valor, out data))
        {
            return true;
        }

        if (DateTime.TryParse(valor, out var dataTime))
        {
            data = DateOnly.FromDateTime(dataTime);
            return true;
        }

        data = default;
        return false;
    }

    private static string? GetValor(ImportacaoLinhaRaw linha, string coluna)
    {
        return linha.Valores.TryGetValue(coluna, out var valor) ? valor : null;
    }

    private static TipoLancamento? ParseTipo(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim().ToLowerInvariant();
        return normalizado switch
        {
            "entrada" or "e" => TipoLancamento.Entrada,
            "saida" or "saída" or "s" => TipoLancamento.Saida,
            _ => null
        };
    }

    private static StatusLancamento? ParseStatus(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim().ToLowerInvariant();
        return normalizado switch
        {
            "previsto" => StatusLancamento.Previsto,
            "realizado" => StatusLancamento.Realizado,
            _ => null
        };
    }
}
