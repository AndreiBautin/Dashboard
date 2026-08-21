using Microsoft.EntityFrameworkCore;
using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class EfMonthlySnapshotRepository : IMonthlySnapshotRepository
{
    private readonly DashboardDbContext _dbContext;

    public EfMonthlySnapshotRepository(DashboardDbContext dbContext)
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
