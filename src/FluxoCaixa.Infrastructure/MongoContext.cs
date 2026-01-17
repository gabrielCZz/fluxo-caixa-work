using MongoDB.Driver;

namespace FluxoCaixa.Infrastructure;

public sealed class MongoOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
}

public interface IMongoRepository
{
    Task InserirLinhasAsync(IEnumerable<ImportacaoRawDocument> linhas, CancellationToken cancellationToken);
    Task<List<ImportacaoRawDocument>> ObterPorImportacaoAsync(Guid importacaoId, CancellationToken cancellationToken);
}

public sealed class MongoRepository : IMongoRepository
{
    private readonly IMongoCollection<ImportacaoRawDocument> _collection;

    public MongoRepository(MongoOptions options)
    {
        var client = new MongoClient(options.ConnectionString);
        var database = client.GetDatabase(options.Database);
        _collection = database.GetCollection<ImportacaoRawDocument>("importacao_raw");
    }

    public Task InserirLinhasAsync(IEnumerable<ImportacaoRawDocument> linhas, CancellationToken cancellationToken)
    {
        return _collection.InsertManyAsync(linhas, cancellationToken: cancellationToken);
    }

    public Task<List<ImportacaoRawDocument>> ObterPorImportacaoAsync(Guid importacaoId, CancellationToken cancellationToken)
    {
        return _collection.Find(x => x.ImportacaoId == importacaoId).ToListAsync(cancellationToken);
    }
}
