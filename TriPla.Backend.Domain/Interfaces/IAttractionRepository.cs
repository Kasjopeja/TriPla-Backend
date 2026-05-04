using TriPla.Backend.Domain.Entities;

namespace TriPla.Backend.Domain.Interfaces;

public interface IAttractionRepository : IRepository<Attraction>
{
    Task<IEnumerable<Attraction>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken = default);
}

