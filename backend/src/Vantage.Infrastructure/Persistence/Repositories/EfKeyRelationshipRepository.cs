using Microsoft.EntityFrameworkCore;
using Vantage.Application.Social;
using Vantage.Domain.Social;

namespace Vantage.Infrastructure.Persistence.Repositories;

public sealed class EfKeyRelationshipRepository : IKeyRelationshipRepository
{
    private readonly VantageDbContext _dbContext;

    public EfKeyRelationshipRepository(VantageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<KeyRelationship>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.KeyRelationships.ToListAsync(cancellationToken);

    public Task<KeyRelationship?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.KeyRelationships.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
}
