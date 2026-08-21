using Microsoft.EntityFrameworkCore;
using Vantage.Application.Social;
using Vantage.Domain.Social;

namespace Vantage.Infrastructure.Persistence.Repositories;

public sealed class EfFriendRepository : IFriendRepository
{
    private readonly VantageDbContext _dbContext;

    public EfFriendRepository(VantageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Friend>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Friends
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

    public Task<Friend?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Friends.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task AddAsync(Friend friend, CancellationToken cancellationToken = default) =>
        await _dbContext.Friends.AddAsync(friend, cancellationToken);
}
