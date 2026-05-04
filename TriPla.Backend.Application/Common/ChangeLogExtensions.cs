using System.Text.Json;
using TriPla.Backend.Domain.Entities;
using TriPla.Backend.Domain.Interfaces;

namespace TriPla.Backend.Application.Common;

public static class ChangeLogExtensions
{
    public static async Task AppendAsync(this IUnitOfWork uow, Guid tripId, string type,
        Guid actorId, object? payload, CancellationToken ct = default)
    {
        var actor = await uow.Users.GetByIdAsync(actorId, ct);
        var json = payload is null ? null : JsonSerializer.Serialize(payload);
        await uow.ChangeLog.AppendAsync(
            new TripChangeLogEntry(tripId, type, actorId, actor?.Email, json, DateTime.UtcNow),
            ct);
    }
}
