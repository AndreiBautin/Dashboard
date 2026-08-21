using Microsoft.EntityFrameworkCore;
using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class EfCategoryRepository : ICategoryRepository
{
    private readonly DashboardDbContext _dbContext;

    public EfCategoryRepository(DashboardDbContext dbContext)
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
