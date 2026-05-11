using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;
using SortDirection = TriPla.Backend.Domain.Entities.SortDirection;

namespace TriPla.Backend.Infrastructure.Repositories;

public class MongoTripChangeLogRepository : ITripChangeLogRepository
{
    private readonly IMongoCollection<ChangeLogDocument> _collection;

    public MongoTripChangeLogRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ChangeLogDocument>("trip_change_log");

        var byTripDate = Builders<ChangeLogDocument>.IndexKeys
            .Ascending(x => x.TripId)
            .Descending(x => x.OccurredAt);
        var byTripTypeDate = Builders<ChangeLogDocument>.IndexKeys
            .Ascending(x => x.TripId)
            .Ascending(x => x.Type)
            .Descending(x => x.OccurredAt);

        _collection.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ChangeLogDocument>(byTripDate),
            new CreateIndexModel<ChangeLogDocument>(byTripTypeDate),
        });
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

    public async Task<IReadOnlyList<TripChangeLogEntry>> QueryAsync(
        ChangeLogQuery query, CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<ChangeLogDocument>.Filter;
        var filter = filterBuilder.Eq(x => x.TripId, query.TripId);

        if (!string.IsNullOrWhiteSpace(query.Type))
            filter &= filterBuilder.Eq(x => x.Type, query.Type);

        if (query.ActorId is { } actorId)
            filter &= filterBuilder.Eq(x => x.ActorId, actorId);

        if (query.From is { } from)
            filter &= filterBuilder.Gte(x => x.OccurredAt, from);

        if (query.To is { } to)
            filter &= filterBuilder.Lte(x => x.OccurredAt, to);

        var sort = BuildSort(query.SortBy, query.SortDirection);

        var docs = await _collection
            .Find(filter)
            .Sort(sort)
            .Skip(query.Skip)
            .Limit(query.Limit)
            .ToListAsync(cancellationToken);

        return docs.Select(d => new TripChangeLogEntry(
            d.TripId,
            d.Type,
            d.ActorId,
            d.ActorEmail,
            d.Payload?.ToJson(),
            d.OccurredAt)).ToList();
    }

    private static SortDefinition<ChangeLogDocument> BuildSort(ChangeLogSortField sortBy, SortDirection direction)
    {
        var builder = Builders<ChangeLogDocument>.Sort;
        return (sortBy, direction) switch
        {
            (ChangeLogSortField.OccurredAt, SortDirection.Ascending) => builder.Ascending(x => x.OccurredAt),
            (ChangeLogSortField.OccurredAt, SortDirection.Descending) => builder.Descending(x => x.OccurredAt),
            (ChangeLogSortField.Type, SortDirection.Ascending) => builder.Ascending(x => x.Type).Descending(x => x.OccurredAt),
            (ChangeLogSortField.Type, SortDirection.Descending) => builder.Descending(x => x.Type).Descending(x => x.OccurredAt),
            (ChangeLogSortField.ActorEmail, SortDirection.Ascending) => builder.Ascending(x => x.ActorEmail).Descending(x => x.OccurredAt),
            (ChangeLogSortField.ActorEmail, SortDirection.Descending) => builder.Descending(x => x.ActorEmail).Descending(x => x.OccurredAt),
            _ => builder.Descending(x => x.OccurredAt),
        };
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
