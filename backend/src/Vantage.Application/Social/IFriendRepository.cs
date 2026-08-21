using Vantage.Domain.Social;

namespace Vantage.Application.Social;

public interface IFriendRepository
{
    Task<IReadOnlyList<Friend>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Friend?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Friend friend, CancellationToken cancellationToken = default);
}
