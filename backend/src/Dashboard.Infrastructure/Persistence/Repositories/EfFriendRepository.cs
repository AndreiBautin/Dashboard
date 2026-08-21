using Microsoft.EntityFrameworkCore;
using Dashboard.Application.Social;
using Dashboard.Domain.Social;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class EfFriendRepository : IFriendRepository
{
    private readonly DashboardDbContext _dbContext;

    public EfFriendRepository(DashboardDbContext dbContext)
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
