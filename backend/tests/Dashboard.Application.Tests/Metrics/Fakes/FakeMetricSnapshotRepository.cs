using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Tests.Metrics.Fakes;

public sealed class FakeMetricSnapshotRepository : IMetricSnapshotRepository
{
    private readonly Dictionary<int, List<MetricSnapshot>> _snapshotsByMetricId = new();
    private readonly Dictionary<int, List<MetricTrendPoint>> _trendByMetricId = new();

    public void SeedSnapshots(int metricDefinitionId, params MetricSnapshot[] orderedSnapshots) =>
        _snapshotsByMetricId[metricDefinitionId] = [.. orderedSnapshots];

    public void SeedTrend(int metricDefinitionId, params MetricTrendPoint[] orderedPoints) =>
        _trendByMetricId[metricDefinitionId] = [.. orderedPoints];

    public Task<IReadOnlyList<MetricSnapshot>> GetForMetricAsync(int metricDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MetricSnapshot>>(
            _snapshotsByMetricId.GetValueOrDefault(metricDefinitionId, []));

    public Task<IReadOnlyList<MetricTrendPoint>> GetTrendForMetricAsync(int metricDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MetricTrendPoint>>(
            _trendByMetricId.GetValueOrDefault(metricDefinitionId, []));
}
