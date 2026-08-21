using Microsoft.EntityFrameworkCore;
using Vantage.Application.Metrics;
using Vantage.Domain.Metrics;

namespace Vantage.Infrastructure.Persistence.Repositories;

public sealed class EfMetricDefinitionRepository : IMetricDefinitionRepository
{
    private readonly VantageDbContext _dbContext;

    public EfMetricDefinitionRepository(VantageDbContext dbContext)
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
