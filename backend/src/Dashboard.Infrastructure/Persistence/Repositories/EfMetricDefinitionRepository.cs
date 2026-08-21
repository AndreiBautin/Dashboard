using Microsoft.EntityFrameworkCore;
using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Infrastructure.Persistence.Repositories;

public sealed class EfMetricDefinitionRepository : IMetricDefinitionRepository
{
    private readonly DashboardDbContext _dbContext;

    public EfMetricDefinitionRepository(DashboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MetricDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.MetricDefinitions
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<MetricDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.MetricDefinitions.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task AddAsync(MetricDefinition metricDefinition, CancellationToken cancellationToken = default) =>
        await _dbContext.MetricDefinitions.AddAsync(metricDefinition, cancellationToken);
}
