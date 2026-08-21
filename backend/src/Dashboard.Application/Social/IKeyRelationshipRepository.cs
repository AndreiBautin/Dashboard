using Dashboard.Domain.Social;

namespace Dashboard.Application.Social;

public interface IKeyRelationshipRepository
{
    Task<IReadOnlyList<KeyRelationship>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<KeyRelationship?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
