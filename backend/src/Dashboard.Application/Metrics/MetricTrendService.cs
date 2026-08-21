namespace Dashboard.Application.Metrics;

/// <summary>Answers "what does this metric look like over time?" for charting.</summary>
public sealed class MetricTrendService
{
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMetricSnapshotRepository _metricSnapshotRepository;

    public MetricTrendService(
        IMetricDefinitionRepository metricDefinitionRepository,
        IMetricSnapshotRepository metricSnapshotRepository)
    {
        _metricDefinitionRepository = metricDefinitionRepository;
        _metricSnapshotRepository = metricSnapshotRepository;
    }

    public async Task<IReadOnlyList<MetricTrendPoint>> GetTrendAsync(
        int metricDefinitionId, CancellationToken cancellationToken = default)
    {
        _ = await _metricDefinitionRepository.GetByIdAsync(metricDefinitionId, cancellationToken)
            ?? throw new InvalidOperationException($"Metric definition {metricDefinitionId} was not found.");

        return await _metricSnapshotRepository.GetTrendForMetricAsync(metricDefinitionId, cancellationToken);
    }
}
