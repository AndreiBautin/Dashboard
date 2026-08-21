using Microsoft.EntityFrameworkCore;
using Vantage.Application.Metrics;
using Vantage.Domain.Metrics;

namespace Vantage.Infrastructure.Persistence.Repositories;

public sealed class EfMonthlySnapshotRepository : IMonthlySnapshotRepository
{
    private readonly VantageDbContext _dbContext;

    public EfMonthlySnapshotRepository(VantageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MonthlySnapshot>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.MonthlySnapshots
            .Include(s => s.MetricSnapshots)
            .Include(s => s.SocialSnapshot)
            .OrderBy(s => s.Month)
            .ToListAsync(cancellationToken);

    public Task<MonthlySnapshot?> GetByMonthAsync(DateOnly month, CancellationToken cancellationToken = default) =>
        _dbContext.MonthlySnapshots
            .Include(s => s.MetricSnapshots)
            .Include(s => s.SocialSnapshot)
            .FirstOrDefaultAsync(s => s.Month == month, cancellationToken);

    public async Task AddAsync(MonthlySnapshot snapshot, CancellationToken cancellationToken = default) =>
        await _dbContext.MonthlySnapshots.AddAsync(snapshot, cancellationToken);
}
