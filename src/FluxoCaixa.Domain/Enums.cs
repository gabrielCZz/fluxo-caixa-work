namespace FluxoCaixa.Domain;

public enum TipoLancamento
{
    Entrada = 1,
    Saida = 2
}

public enum StatusLancamento
{
    Previsto = 1,
    Realizado = 2
}

public enum OrigemLancamento
{
    Importado = 1,
    Manual = 2
}

public enum StatusImportacao
{
    Processando = 1,
    Concluida = 2,
    Falha = 3
}

public enum ModoMatch
{
    Exato = 1,
    Contem = 2
}
