using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FluxoCaixa.Infrastructure;

public sealed class ImportacaoRawDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public Guid ImportacaoId { get; set; }
    public int Linha { get; set; }
    public Dictionary<string, string?> Valores { get; set; } = new();
    public List<string> Erros { get; set; } = new();
}
