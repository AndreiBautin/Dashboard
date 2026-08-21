using Microsoft.EntityFrameworkCore;
using Dashboard.Application.Social;
using Dashboard.Domain.Social;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class EfKeyRelationshipRepository : IKeyRelationshipRepository
{
    private readonly DashboardDbContext _dbContext;

    public EfKeyRelationshipRepository(DashboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<KeyRelationship>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.KeyRelationships.ToListAsync(cancellationToken);

    public Task<KeyRelationship?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.KeyRelationships.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
}
