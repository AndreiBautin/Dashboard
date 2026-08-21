using Microsoft.EntityFrameworkCore;
using Vantage.Application.Metrics;
using Vantage.Domain.Metrics;

namespace Vantage.Infrastructure.Persistence.Repositories;

public sealed class EfCategoryRepository : ICategoryRepository
{
    private readonly VantageDbContext _dbContext;

    public EfCategoryRepository(VantageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Categories
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        await _dbContext.Categories.AddAsync(category, cancellationToken);
}
