using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Infrastructure.Repositories;

public class MongoTripChangeLogRepository : ITripChangeLogRepository
{
    private readonly IMongoCollection<ChangeLogDocument> _collection;

    public MongoTripChangeLogRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ChangeLogDocument>("trip_change_log");

        var indexKeys = Builders<ChangeLogDocument>.IndexKeys
            .Ascending(x => x.TripId)
            .Descending(x => x.OccurredAt);
        _collection.Indexes.CreateOne(new CreateIndexModel<ChangeLogDocument>(indexKeys));
    }

    public async Task AppendAsync(TripChangeLogEntry entry, CancellationToken cancellationToken = default)
    {
        var doc = new ChangeLogDocument
        {
            TripId = entry.TripId,
            Type = entry.Type,
            ActorId = entry.ActorId,
            ActorEmail = entry.ActorEmail,
            Payload = string.IsNullOrWhiteSpace(entry.PayloadJson)
                ? null
                : BsonDocument.Parse(entry.PayloadJson),
            OccurredAt = entry.OccurredAt,
        };

        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<TripChangeLogEntry>> GetByTripIdAsync(
        Guid tripId, int limit = 100, CancellationToken cancellationToken = default)
    {
        var docs = await _collection
            .Find(x => x.TripId == tripId)
            .SortByDescending(x => x.OccurredAt)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return docs.Select(d => new TripChangeLogEntry(
            d.TripId,
            d.Type,
            d.ActorId,
            d.ActorEmail,
            d.Payload?.ToJson(),
            d.OccurredAt)).ToList();
    }

    private class ChangeLogDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("tripId")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid TripId { get; set; }

        [BsonElement("type")]
        public string Type { get; set; } = default!;

        [BsonElement("actorId")]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid ActorId { get; set; }

        [BsonElement("actorEmail")]
        public string? ActorEmail { get; set; }

        [BsonElement("payload")]
        public BsonDocument? Payload { get; set; }

        [BsonElement("occurredAt")]
        public DateTime OccurredAt { get; set; }
    }
}
