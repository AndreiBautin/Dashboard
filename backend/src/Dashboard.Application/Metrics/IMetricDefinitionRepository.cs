using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Metrics;

public interface IMetricDefinitionRepository
{
    Task<IReadOnlyList<MetricDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MetricDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(MetricDefinition metricDefinition, CancellationToken cancellationToken = default);
}
