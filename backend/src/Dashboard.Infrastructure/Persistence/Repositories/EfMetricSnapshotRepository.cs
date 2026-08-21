using Microsoft.EntityFrameworkCore;
using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class EfMetricSnapshotRepository : IMetricSnapshotRepository
{
    private readonly DashboardDbContext _dbContext;

    public EfMetricSnapshotRepository(DashboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <remarks>
    /// Ordered by the review month the snapshot belongs to, not by
    /// RecordedAt — RecordedAt is just data-entry metadata and shouldn't
    /// drive evaluation order (e.g. a backfilled entry recorded late).
    /// </remarks>
    public async Task<IReadOnlyList<MetricSnapshot>> GetForMetricAsync(
        int metricDefinitionId, CancellationToken cancellationToken = default)
    {
        return await (
            from snapshot in _dbContext.MetricSnapshots
            join monthlySnapshot in _dbContext.MonthlySnapshots
                on snapshot.MonthlySnapshotId equals monthlySnapshot.Id
            where snapshot.MetricDefinitionId == metricDefinitionId
            orderby monthlySnapshot.Month
            select snapshot
        ).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetricTrendPoint>> GetTrendForMetricAsync(
        int metricDefinitionId, CancellationToken cancellationToken = default)
    {
        return await (
            from snapshot in _dbContext.MetricSnapshots
            join monthlySnapshot in _dbContext.MonthlySnapshots
                on snapshot.MonthlySnapshotId equals monthlySnapshot.Id
            where snapshot.MetricDefinitionId == metricDefinitionId
            orderby monthlySnapshot.Month
            select new MetricTrendPoint(monthlySnapshot.Month, snapshot.Value)
        ).ToListAsync(cancellationToken);
    }
}
